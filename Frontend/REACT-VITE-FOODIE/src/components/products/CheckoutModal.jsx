import React, { useEffect, useState } from 'react';
import { Modal, Form, Input, Radio, Checkbox, Card, Typography, Flex, Spin } from 'antd';
import { UserOutlined, PhoneOutlined, EnvironmentOutlined } from '@ant-design/icons';
import { orderService } from '../../services/auth.service';
import { profileService } from '../../services/profile.service';
import { formatVND } from '../../utils/helpers';

const { Text } = Typography;
const PHONE_RULE = { pattern: /^(0|\+84)\d{9}$/, message: "Số điện thoại chưa đúng (ví dụ 0901234567)." };

/**
 * Hộp thanh toán. Có địa chỉ mặc định => chọn sẵn; hoặc "Giao tới địa chỉ khác" (nhập mới,
 * có thể lưu làm mặc định). Giá và phí ship do server tính lại, không tin số tiền gửi lên.
 */
const CheckoutModal = ({ open, onClose, cart, total, user, onSuccess, notify }) => {
    const [form] = Form.useForm();
    const [submitting, setSubmitting] = useState(false);
    const [loadingAddress, setLoadingAddress] = useState(false);
    const [saved, setSaved] = useState(null);          // địa chỉ mặc định { recipientName, phone, address } | null
    const [mode, setMode] = useState("new");           // "default" | "new"

    // Mỗi lần mở: lấy địa chỉ mặc định mới nhất từ hồ sơ
    useEffect(() => {
        if (!open || !user) return undefined;
        let ignore = false;
        setLoadingAddress(true);
        profileService
            .get()
            .then((p) => {
                if (ignore) return;
                setSaved(p.defaultAddress);
                setMode(p.defaultAddress ? "default" : "new");
                form.setFieldsValue({ saveAsDefault: !p.defaultAddress });
            })
            .catch(() => { if (!ignore) { setSaved(null); setMode("new"); } })
            .finally(() => !ignore && setLoadingAddress(false));
        return () => { ignore = true; };
    }, [open, user, form]);

    const handleFinish = async (values) => {
        const useDefault = mode === "default" && saved;
        const shipTo = useDefault
            ? saved
            : { recipientName: values.recipientName.trim(), phone: values.phone.trim(), address: values.address.trim() };

        setSubmitting(true);
        try {
            const result = await orderService.create({
                items: cart.map((i) => ({ productId: i.id, quantity: i.qty })),
                receiverName: shipTo.recipientName,
                phone: shipTo.phone,
                address: shipTo.address,
                note: values.note,
                paymentMethod: values.paymentMethod,
            });

            // Lưu địa chỉ mới làm mặc định (không chặn đơn hàng nếu bước này lỗi)
            if (!useDefault && values.saveAsDefault) {
                profileService.saveAddress(shipTo).catch(() => notify.warning("Đặt hàng thành công nhưng chưa lưu được địa chỉ mặc định."));
            }

            form.resetFields();
            onSuccess(result);
        } catch (e) {
            notify.error(e.message);
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <Modal
            open={open}
            onCancel={onClose}
            title="Thông tin giao hàng"
            okText={`Đặt hàng - ${formatVND(total)}`}
            cancelText="Quay lại"
            confirmLoading={submitting}
            okButtonProps={{ disabled: loadingAddress }}
            onOk={() => form.submit()}
            centered
            destroyOnClose
        >
            <Spin spinning={loadingAddress}>
                <Form
                    form={form}
                    layout="vertical"
                    onFinish={handleFinish}
                    initialValues={{ recipientName: user?.name, paymentMethod: "cod", saveAsDefault: true }}
                    style={{ marginTop: 16 }}
                >
                    {saved && (
                        <Radio.Group value={mode} onChange={(e) => setMode(e.target.value)} style={{ width: "100%", marginBottom: 16 }}>
                            <Flex vertical gap={8}>
                                <Card size="small" style={{ borderColor: mode === "default" ? "#E3121B" : undefined }}>
                                    <Radio value="default">
                                        <Text strong>Địa chỉ mặc định</Text>
                                        <div style={{ fontSize: 13, color: "#555", marginTop: 2 }}>
                                            {saved.recipientName} · {saved.phone}
                                            <br />
                                            {saved.address}
                                        </div>
                                    </Radio>
                                </Card>
                                <Card size="small" style={{ borderColor: mode === "new" ? "#E3121B" : undefined }}>
                                    <Radio value="new"><Text strong>Giao tới địa chỉ khác</Text></Radio>
                                </Card>
                            </Flex>
                        </Radio.Group>
                    )}

                    {/* Chỉ hiện (và kiểm tra) các ô địa chỉ khi nhập địa chỉ mới */}
                    {mode === "new" && (
                        <>
                            <Form.Item
                                name="recipientName"
                                label="Người nhận"
                                rules={[{ required: true, whitespace: true, message: "Vui lòng nhập tên người nhận." }, { min: 2, max: 100 }]}
                            >
                                <Input prefix={<UserOutlined />} placeholder="Nguyễn Văn A" maxLength={100} />
                            </Form.Item>
                            <Form.Item name="phone" label="Số điện thoại" rules={[{ required: true, message: "Vui lòng nhập số điện thoại." }, PHONE_RULE]}>
                                <Input prefix={<PhoneOutlined />} placeholder="0901234567" maxLength={12} />
                            </Form.Item>
                            <Form.Item
                                name="address"
                                label="Địa chỉ"
                                rules={[{ required: true, whitespace: true, message: "Vui lòng nhập địa chỉ giao hàng." }, { max: 250 }]}
                            >
                                <Input prefix={<EnvironmentOutlined />} placeholder="Số nhà, đường, phường/xã, quận/huyện" maxLength={250} />
                            </Form.Item>
                            <Form.Item name="saveAsDefault" valuePropName="checked" style={{ marginTop: -8 }}>
                                <Checkbox>{saved ? "Đặt làm địa chỉ mặc định mới" : "Lưu làm địa chỉ mặc định"}</Checkbox>
                            </Form.Item>
                        </>
                    )}

                    <Form.Item name="note" label="Ghi chú cho bếp" rules={[{ max: 500 }]}>
                        <Input.TextArea rows={2} placeholder="Ví dụ: ít cay, không hành" maxLength={500} />
                    </Form.Item>
                    <Form.Item name="paymentMethod" label="Thanh toán">
                        <Radio.Group>
                            <Radio value="cod">Tiền mặt khi nhận</Radio>
                            <Radio value="bank">Chuyển khoản</Radio>
                        </Radio.Group>
                    </Form.Item>
                </Form>
            </Spin>
        </Modal>
    );
};

export default CheckoutModal;
