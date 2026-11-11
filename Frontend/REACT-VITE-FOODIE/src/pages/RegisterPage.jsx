import React, { useState } from 'react';
import { Alert, Form, Input, Button, Typography } from 'antd';
import { UserOutlined, MailOutlined, LockOutlined } from '@ant-design/icons';
import { authService } from '../services/auth.service';
import { AuthShell } from './LoginPage'; // Nếu export AuthShell từ LoginPage hoặc tách riêng

const { Text } = Typography;

const RegisterPage = ({ setView, onAuthSuccess }) => {
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState("");

    const handleFinish = async ({ name, email, password }) => {
        setLoading(true);
        setError("");
        try {
            const data = await authService.register({ name: name.trim(), email, password });
            onAuthSuccess(data, "register");
        } catch (e) {
            setError(e.message);
        } finally {
            setLoading(false);
        }
    };

    return (
        <AuthShell
            title="Đăng ký"
            subtitle="Chỉ mất chưa tới một phút."
            side={{ title: "Tạo tài khoản Foodie", text: "Nhận ưu đãi 20% cho đơn đầu tiên và lưu lại món yêu thích." }}
        >
            {error && <Alert type="error" showIcon message={error} style={{ marginBottom: 16 }} />}

            <Form layout="vertical" onFinish={handleFinish} requiredMark={false} size="large">
                <Form.Item
                    name="name"
                    label="Họ và tên"
                    rules={[
                        { required: true, message: "Vui lòng nhập họ tên." },
                        { min: 2, message: "Họ tên cần ít nhất 2 ký tự." },
                    ]}
                >
                    <Input prefix={<UserOutlined />} placeholder="Nguyễn Văn A" autoComplete="name" />
                </Form.Item>
                <Form.Item
                    name="email"
                    label="Email"
                    rules={[
                        { required: true, message: "Vui lòng nhập email." },
                        { type: "email", message: "Email chưa đúng định dạng." },
                    ]}
                >
                    <Input prefix={<MailOutlined />} placeholder="ban@example.com" autoComplete="email" />
                </Form.Item>
                <Form.Item
                    name="password"
                    label="Mật khẩu"
                    rules={[
                        { required: true, message: "Vui lòng nhập mật khẩu." },
                        { min: 6, message: "Mật khẩu cần ít nhất 6 ký tự." },
                    ]}
                >
                    <Input.Password prefix={<LockOutlined />} placeholder="Ít nhất 6 ký tự" autoComplete="new-password" />
                </Form.Item>
                <Form.Item
                    name="confirm"
                    label="Nhập lại mật khẩu"
                    dependencies={["password"]}
                    rules={[
                        { required: true, message: "Vui lòng nhập lại mật khẩu." },
                        ({ getFieldValue }) => ({
                            validator(_, value) {
                                if (!value || getFieldValue("password") === value) return Promise.resolve();
                                return Promise.reject(new Error("Mật khẩu nhập lại chưa khớp."));
                            },
                        }),
                    ]}
                >
                    <Input.Password prefix={<LockOutlined />} placeholder="Nhập lại mật khẩu" autoComplete="new-password" />
                </Form.Item>
                <Button type="primary" htmlType="submit" block loading={loading}>Tạo tài khoản</Button>
            </Form>

            <div style={{ textAlign: "center", marginTop: 16 }}>
                <Text type="secondary">Đã có tài khoản? </Text>
                <Button type="link" style={{ padding: 0, fontWeight: 600 }} onClick={() => setView("login")}>Đăng nhập</Button>
            </div>
        </AuthShell>
    );
}


export default RegisterPage;