import React, { useEffect, useState } from 'react';
import {
    Typography, Tabs, Form, Input, Button, Avatar, Upload, Flex, Alert, Spin, App as AntApp, Card, Popconfirm,
} from 'antd';
import {
    UserOutlined, CameraOutlined, DeleteOutlined, LockOutlined, PhoneOutlined, EnvironmentOutlined,
} from '@ant-design/icons';
import Container from '../components/Container';
import { profileService } from '../services/profile.service';
import { PRIMARY, display, resolveImageUrl, formatDateTime } from '../utils/helpers';

const { Title, Text } = Typography;
const MAX_AVATAR_BYTES = 2 * 1024 * 1024;
const PHONE_RULE = { pattern: /^(0|\+84)\d{9}$/, message: 'Số điện thoại chưa đúng (ví dụ 0901234567).' };

/** Tab "Thông tin": ảnh đại diện + họ tên. */
const InfoTab = ({ profile, onChanged }) => {
    const { message } = AntApp.useApp();
    const [form] = Form.useForm();
    const [saving, setSaving] = useState(false);
    const [uploading, setUploading] = useState(false);

    const saveName = async ({ name }) => {
        setSaving(true);
        try {
            onChanged(await profileService.updateName(name.trim()));
            message.success('Đã lưu họ tên.');
        } catch (e) {
            message.error(e.message);
        } finally {
            setSaving(false);
        }
    };

    // Kiểm tra nhanh phía trình duyệt; server vẫn kiểm tra lại đuôi file, dung lượng, nội dung thật
    const beforeUpload = (file) => {
        if (!['image/jpeg', 'image/png'].includes(file.type)) {
            message.error('Chỉ chấp nhận ảnh JPG hoặc PNG.');
            return Upload.LIST_IGNORE;
        }
        if (file.size > MAX_AVATAR_BYTES) {
            message.error('Ảnh vượt quá 2MB, vui lòng chọn ảnh nhỏ hơn.');
            return Upload.LIST_IGNORE;
        }
        return true;
    };

    const upload = async ({ file, onSuccess, onError }) => {
        setUploading(true);
        try {
            const updated = await profileService.uploadAvatar(file);
            onChanged(updated);
            message.success('Đã đổi ảnh đại diện.');
            onSuccess(updated);
        } catch (e) {
            message.error(e.message);
            onError(e);
        } finally {
            setUploading(false);
        }
    };

    const removeAvatar = async () => {
        try {
            onChanged(await profileService.removeAvatar());
            message.success('Đã gỡ ảnh đại diện.');
        } catch (e) {
            message.error(e.message);
        }
    };

    return (
        <Flex vertical gap={28}>
            <Flex align="center" gap={20} wrap>
                <Avatar
                    size={96}
                    src={resolveImageUrl(profile.avatarUrl)}
                    icon={<UserOutlined />}
                    style={{ background: profile.avatarUrl ? '#fff' : PRIMARY, flexShrink: 0 }}
                />
                <Flex vertical gap={8}>
                    <Flex gap={8} wrap>
                        <Upload accept=".jpg,.jpeg,.png" showUploadList={false} beforeUpload={beforeUpload} customRequest={upload}>
                            <Button icon={<CameraOutlined />} loading={uploading}>Đổi ảnh</Button>
                        </Upload>
                        {profile.avatarUrl && (
                            <Popconfirm title="Gỡ ảnh đại diện?" okText="Gỡ" cancelText="Hủy" onConfirm={removeAvatar}>
                                <Button icon={<DeleteOutlined />} danger>Gỡ ảnh</Button>
                            </Popconfirm>
                        )}
                    </Flex>
                    <Text type="secondary" style={{ fontSize: 13 }}>JPG hoặc PNG, tối đa 2MB.</Text>
                </Flex>
            </Flex>

            <Form form={form} layout="vertical" initialValues={{ name: profile.name }} onFinish={saveName} style={{ maxWidth: 440 }}>
                <Form.Item label="Email">
                    <Input value={profile.email} disabled />
                </Form.Item>
                <Form.Item
                    name="name"
                    label="Họ tên"
                    rules={[
                        { required: true, whitespace: true, message: 'Vui lòng nhập họ tên.' },
                        { min: 2, max: 100, message: 'Họ tên cần từ 2 đến 100 ký tự.' },
                    ]}
                >
                    <Input prefix={<UserOutlined />} maxLength={100} />
                </Form.Item>
                <Text type="secondary" style={{ display: 'block', marginBottom: 16, fontSize: 13 }}>
                    Thành viên từ {formatDateTime(profile.createdAt)}
                </Text>
                <Button type="primary" htmlType="submit" loading={saving}>Lưu thay đổi</Button>
            </Form>
        </Flex>
    );
};

/** Tab "Địa chỉ mặc định": dùng để điền sẵn khi thanh toán. */
const AddressTab = ({ profile, onChanged }) => {
    const { message } = AntApp.useApp();
    const [form] = Form.useForm();
    const [saving, setSaving] = useState(false);

    const save = async (values) => {
        setSaving(true);
        try {
            onChanged(await profileService.saveAddress(values));
            message.success('Đã lưu địa chỉ mặc định.');
        } catch (e) {
            message.error(e.message);
        } finally {
            setSaving(false);
        }
    };

    const clear = async () => {
        try {
            onChanged(await profileService.clearAddress());
            form.resetFields();
            form.setFieldsValue({ recipientName: '', phone: '', address: '' });
            message.success('Đã xóa địa chỉ mặc định.');
        } catch (e) {
            message.error(e.message);
        }
    };

    return (
        <Form
            form={form}
            layout="vertical"
            initialValues={profile.defaultAddress ?? { recipientName: profile.name }}
            onFinish={save}
            style={{ maxWidth: 520 }}
        >
            <Alert
                type="info"
                showIcon
                style={{ marginBottom: 20 }}
                message="Địa chỉ mặc định được chọn sẵn khi thanh toán. Bạn vẫn có thể giao tới địa chỉ khác cho từng đơn."
            />
            <Form.Item
                name="recipientName"
                label="Người nhận"
                rules={[{ required: true, whitespace: true, message: 'Vui lòng nhập tên người nhận.' }, { min: 2, max: 100 }]}
            >
                <Input prefix={<UserOutlined />} maxLength={100} />
            </Form.Item>
            <Form.Item name="phone" label="Số điện thoại" rules={[{ required: true, message: 'Vui lòng nhập số điện thoại.' }, PHONE_RULE]}>
                <Input prefix={<PhoneOutlined />} placeholder="0901234567" maxLength={12} />
            </Form.Item>
            <Form.Item
                name="address"
                label="Địa chỉ"
                rules={[{ required: true, whitespace: true, message: 'Vui lòng nhập địa chỉ.' }, { max: 250 }]}
            >
                <Input.TextArea rows={2} placeholder="Số nhà, đường, phường/xã, quận/huyện" maxLength={250} />
            </Form.Item>
            <Flex gap={8} wrap>
                <Button type="primary" htmlType="submit" icon={<EnvironmentOutlined />} loading={saving}>Lưu địa chỉ</Button>
                {profile.defaultAddress && (
                    <Popconfirm title="Xóa địa chỉ mặc định?" okText="Xóa" cancelText="Hủy" onConfirm={clear}>
                        <Button danger>Xóa địa chỉ</Button>
                    </Popconfirm>
                )}
            </Flex>
        </Form>
    );
};

/** Tab "Đổi mật khẩu". */
const PasswordTab = () => {
    const { message } = AntApp.useApp();
    const [form] = Form.useForm();
    const [saving, setSaving] = useState(false);

    const save = async (values) => {
        setSaving(true);
        try {
            await profileService.changePassword(values);
            form.resetFields();
            message.success('Đã đổi mật khẩu.');
        } catch (e) {
            message.error(e.message);
        } finally {
            setSaving(false);
        }
    };

    return (
        <Form form={form} layout="vertical" onFinish={save} style={{ maxWidth: 440 }} autoComplete="off">
            <Form.Item name="currentPassword" label="Mật khẩu hiện tại" rules={[{ required: true, message: 'Vui lòng nhập mật khẩu hiện tại.' }]}>
                <Input.Password prefix={<LockOutlined />} autoComplete="current-password" />
            </Form.Item>
            <Form.Item
                name="newPassword"
                label="Mật khẩu mới"
                rules={[
                    { required: true, message: 'Vui lòng nhập mật khẩu mới.' },
                    { min: 6, max: 72, message: 'Mật khẩu mới cần từ 6 đến 72 ký tự.' },
                ]}
                hasFeedback
            >
                <Input.Password prefix={<LockOutlined />} autoComplete="new-password" />
            </Form.Item>
            <Form.Item
                name="confirmPassword"
                label="Nhập lại mật khẩu mới"
                dependencies={['newPassword']}
                hasFeedback
                rules={[
                    { required: true, message: 'Vui lòng nhập lại mật khẩu mới.' },
                    ({ getFieldValue }) => ({
                        validator: (_, value) =>
                            !value || getFieldValue('newPassword') === value
                                ? Promise.resolve()
                                : Promise.reject(new Error('Mật khẩu nhập lại không khớp.')),
                    }),
                ]}
            >
                <Input.Password prefix={<LockOutlined />} autoComplete="new-password" />
            </Form.Item>
            <Button type="primary" htmlType="submit" loading={saving}>Đổi mật khẩu</Button>
        </Form>
    );
};

/**
 * Trang "Tài khoản của tôi".
 *  - onUserChanged(changes): cập nhật tên / ảnh đại diện trên header
 */
const ProfilePage = ({ onUserChanged }) => {
    const [profile, setProfile] = useState(null);
    const [error, setError] = useState('');

    useEffect(() => {
        let ignore = false;
        profileService
            .get()
            .then((p) => !ignore && setProfile(p))
            .catch((e) => !ignore && setError(e.message));
        return () => { ignore = true; };
    }, []);

    const handleChanged = (p) => {
        setProfile(p);
        onUserChanged({ name: p.name, avatarUrl: p.avatarUrl });
    };

    return (
        <Container style={{ paddingBottom: 24, maxWidth: 820 }}>
            <div style={{ padding: '36px 0 20px' }}>
                <Title level={1} style={{ ...display, margin: 0 }}>Tài khoản của tôi</Title>
                <Text type="secondary">Quản lý thông tin cá nhân, địa chỉ giao hàng và mật khẩu.</Text>
            </div>

            {error && <Alert type="error" showIcon message="Không tải được hồ sơ" description={error} />}
            {!error && !profile && <Flex justify="center" style={{ padding: 60 }}><Spin /></Flex>}
            {profile && (
                <Card styles={{ body: { paddingTop: 8 } }}>
                    <Tabs
                        items={[
                            { key: 'info', label: 'Thông tin', children: <InfoTab profile={profile} onChanged={handleChanged} /> },
                            { key: 'address', label: 'Địa chỉ mặc định', children: <AddressTab profile={profile} onChanged={handleChanged} /> },
                            { key: 'password', label: 'Đổi mật khẩu', children: <PasswordTab /> },
                        ]}
                    />
                </Card>
            )}
        </Container>
    );
};

export default ProfilePage;
