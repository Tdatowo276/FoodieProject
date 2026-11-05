import React, { useState } from 'react';
import { Modal, Form, Input, Radio } from 'antd';
import { UserOutlined, PhoneOutlined, EnvironmentOutlined } from '@ant-design/icons';
import { orderService } from '../../services/auth.service'; // Hoặc import từ file orderService riêng
import { formatVND } from '../../utils/helpers';

const CheckoutModal = ({ open, onClose, cart, total, shipping, user, onSuccess, notify }) => {
    const [form] = Form.useForm();
    const [submitting, setSubmitting] = useState(false);

    const handleFinish = async (values) => {
        setSubmitting(true);
        try {
            const result = await orderService.create({
                items: cart.map((i) => ({ productId: i.id, quantity: i.qty })),
                shippingFee: shipping,
                total,
                receiverName: values.receiverName,
                phone: values.phone,
                address: values.address,
                note: values.note,
                paymentMethod: values.paymentMethod,
            });
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
            onOk={() => form.submit()}
            centered
        >
            <Form
                form={form}
                layout="vertical"
                onFinish={handleFinish}
                initialValues={{ receiverName: user?.name, paymentMethod: "cod" }}
                style={{ marginTop: 16 }}
            >
                <Form.Item name="receiverName" label="Người nhận" rules={[{ required: true, message: "Vui lòng nhập tên người nhận." }]}>
                    <Input prefix={<UserOutlined />} placeholder="Nguyễn Văn A" />
                </Form.Item>
                <Form.Item
                    name="phone"
                    label="Số điện thoại"
                    rules={[
                        { required: true, message: "Vui lòng nhập số điện thoại." },
                        { pattern: /^(0|\+84)\d{9}$/, message: "Số điện thoại chưa đúng (ví dụ 0901234567)." },
                    ]}
                >
                    <Input prefix={<PhoneOutlined />} placeholder="0901234567" />
                </Form.Item>
                <Form.Item name="address" label="Địa chỉ" rules={[{ required: true, message: "Vui lòng nhập địa chỉ giao hàng." }]}>
                    <Input prefix={<EnvironmentOutlined />} placeholder="Số nhà, đường, phường/xã, quận/huyện" />
                </Form.Item>
                <Form.Item name="note" label="Ghi chú cho bếp">
                    <Input.TextArea rows={2} placeholder="Ví dụ: ít cay, không hành" />
                </Form.Item>
                <Form.Item name="paymentMethod" label="Thanh toán">
                    <Radio.Group>
                        <Radio value="cod">Tiền mặt khi nhận</Radio>
                        <Radio value="bank">Chuyển khoản</Radio>
                    </Radio.Group>
                </Form.Item>
            </Form>
        </Modal>
    );
}

export default CheckoutModal;