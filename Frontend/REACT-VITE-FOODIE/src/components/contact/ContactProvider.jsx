import React, { useMemo, useState } from 'react';
import { Modal, Button, Typography, Flex, Form, Input, Select, Rate, Result, App as AntApp } from 'antd';
import { PhoneFilled, EnvironmentOutlined, ClockCircleOutlined, SendOutlined, ShopOutlined } from '@ant-design/icons';
import ShopMap from './ShopMap';
import contact, { googleMapsUrl } from '../../config/contact';
import { feedbackService, FEEDBACK_TOPICS } from '../../services/feedback.service';
import { PRIMARY } from '../../utils/helpers';
import { ContactContext } from './contactContext';

const { Text, Paragraph, Title } = Typography;

const PHONE_RULE = { pattern: /^(0|\+84)\d{9}$/, message: 'Số điện thoại chưa đúng (ví dụ 0901234567).' };

/** Hộp thông báo số điện thoại góp ý. */
const PhoneModal = ({ open, onClose }) => (
    <Modal open={open} onCancel={onClose} footer={null} centered width={420}>
        <Flex vertical align="center" gap={12} style={{ textAlign: 'center', padding: '12px 4px 4px' }}>
            <div style={{ width: 64, height: 64, borderRadius: '50%', background: PRIMARY, display: 'grid', placeItems: 'center' }}>
                <PhoneFilled style={{ color: '#fff', fontSize: 30 }} />
            </div>
            <Title level={4} style={{ margin: 0 }}>Liên hệ góp ý</Title>
            <Paragraph style={{ margin: 0, fontSize: 16 }}>
                Có góp ý xin vui lòng liên hệ số điện thoại
                <br />
                <Text strong style={{ fontSize: 22, color: PRIMARY }}>{contact.phoneDisplay}</Text>
            </Paragraph>
            <Text type="secondary"><ClockCircleOutlined /> {contact.openingHours}</Text>
            <Flex gap={8} style={{ marginTop: 8 }}>
                {/* Trên điện thoại: bấm để gọi; trên máy tính: mở ứng dụng gọi điện nếu có */}
                <Button type="primary" icon={<PhoneFilled />} href={`tel:${contact.phone}`}>Gọi ngay</Button>
                <Button onClick={onClose}>Đóng</Button>
            </Flex>
        </Flex>
    </Modal>
);

/** Hộp bản đồ lớn: vị trí shop có biểu tượng ngôi nhà. */
const MapModal = ({ open, onClose }) => (
    <Modal open={open} onCancel={onClose} footer={null} centered width={760} title="Vị trí cửa hàng" destroyOnClose>
        <ShopMap height="min(60vh, 440px)" zoom={17} showPopup />
        <Flex justify="space-between" align="center" wrap gap={12} style={{ marginTop: 16 }}>
            <Flex vertical gap={4}>
                <Text><ShopOutlined style={{ color: PRIMARY }} /> <Text strong>{contact.shopName}</Text></Text>
                <Text><EnvironmentOutlined style={{ color: PRIMARY }} /> {contact.address}</Text>
                <Text type="secondary"><ClockCircleOutlined /> {contact.openingHours}</Text>
            </Flex>
            <Button type="primary" icon={<EnvironmentOutlined />} href={googleMapsUrl} target="_blank" rel="noopener noreferrer">
                Chỉ đường (Google Maps)
            </Button>
        </Flex>
    </Modal>
);

/** Hộp gửi góp ý cho chủ shop (lưu vào CSDL, chủ shop xem ở trang Admin > Góp ý). */
const FeedbackModal = ({ open, onClose, user }) => {
    const { message } = AntApp.useApp();
    const [form] = Form.useForm();
    const [sending, setSending] = useState(false);
    const [done, setDone] = useState(false);

    const close = () => {
        onClose();
        setDone(false);
    };

    const submit = async (values) => {
        setSending(true);
        try {
            await feedbackService.send({ ...values, name: values.name.trim(), message: values.message.trim() });
            form.resetFields();
            setDone(true);
        } catch (e) {
            message.error(e.message);
        } finally {
            setSending(false);
        }
    };

    return (
        <Modal
            open={open}
            onCancel={close}
            title={done ? null : 'Gửi góp ý cho Foodie'}
            okText="Gửi góp ý"
            okButtonProps={{ icon: <SendOutlined />, loading: sending }}
            cancelText="Hủy"
            onOk={() => form.submit()}
            footer={done ? null : undefined}
            centered
            width={520}
        >
            {done ? (
                <Result
                    status="success"
                    title="Cảm ơn bạn đã góp ý!"
                    subTitle="Foodie đã nhận được ý kiến của bạn và sẽ phản hồi qua email sớm nhất."
                    extra={<Button type="primary" onClick={close}>Đóng</Button>}
                />
            ) : (
                <Form
                    form={form}
                    layout="vertical"
                    onFinish={submit}
                    initialValues={{ name: user?.name, email: user?.email, topic: 'food' }}
                    style={{ marginTop: 12 }}
                    requiredMark="optional"
                >
                    <Flex gap={12} wrap>
                        <Form.Item
                            name="name"
                            label="Họ tên"
                            style={{ flex: '1 1 200px' }}
                            rules={[{ required: true, whitespace: true, message: 'Vui lòng nhập họ tên.' }, { min: 2, max: 100 }]}
                        >
                            <Input maxLength={100} />
                        </Form.Item>
                        <Form.Item name="phone" label="Số điện thoại" style={{ flex: '1 1 160px' }} rules={[PHONE_RULE]}>
                            <Input placeholder="Không bắt buộc" maxLength={12} />
                        </Form.Item>
                    </Flex>
                    <Form.Item
                        name="email"
                        label="Email (để chủ shop phản hồi)"
                        rules={[{ required: true, message: 'Vui lòng nhập email.' }, { type: 'email', message: 'Email chưa đúng định dạng.' }]}
                    >
                        <Input maxLength={254} />
                    </Form.Item>
                    <Flex gap={12} wrap align="end">
                        <Form.Item name="topic" label="Chủ đề" style={{ flex: '1 1 200px' }} rules={[{ required: true }]}>
                            <Select options={FEEDBACK_TOPICS} />
                        </Form.Item>
                        <Form.Item name="rating" label="Đánh giá" style={{ flex: '1 1 160px' }}>
                            <Rate />
                        </Form.Item>
                    </Flex>
                    <Form.Item
                        name="message"
                        label="Nội dung"
                        rules={[
                            { required: true, whitespace: true, message: 'Vui lòng nhập nội dung góp ý.' },
                            { min: 10, max: 1000, message: 'Nội dung cần từ 10 đến 1000 ký tự.' },
                        ]}
                    >
                        <Input.TextArea rows={4} showCount maxLength={1000} placeholder="Món ăn, giao hàng, phục vụ... bạn thấy thế nào?" />
                    </Form.Item>
                </Form>
            )}
        </Modal>
    );
};

/** Bọc ứng dụng: cung cấp openMap / openPhone / openFeedback và hiển thị các hộp tương ứng. */
export const ContactProvider = ({ user, children }) => {
    const [opened, setOpened] = useState(null);   // 'map' | 'phone' | 'feedback' | null

    const api = useMemo(() => ({
        openMap: () => setOpened('map'),
        openPhone: () => setOpened('phone'),
        openFeedback: () => setOpened('feedback'),
    }), []);

    const close = () => setOpened(null);

    return (
        <ContactContext.Provider value={api}>
            {children}
            <PhoneModal open={opened === 'phone'} onClose={close} />
            <MapModal open={opened === 'map'} onClose={close} />
            <FeedbackModal open={opened === 'feedback'} onClose={close} user={user} />
        </ContactContext.Provider>
    );
};
