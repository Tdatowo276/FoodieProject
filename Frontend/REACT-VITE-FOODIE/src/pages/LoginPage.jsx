import React, { useState } from 'react';
import { Card, Row, Col, Typography, Alert, Form, Input, Button } from 'antd';
import { MailOutlined, LockOutlined } from '@ant-design/icons';
import { authService } from '../services/auth.service';
import { display, PRIMARY } from '../utils/helpers';

const { Title, Text } = Typography;
const USE_MOCK = import.meta.env.VITE_USE_MOCK !== "false";

const AuthShell = ({ title, subtitle, side, children }) => {
    return (
        <div style={{ display: "grid", placeItems: "center", padding: "40px 20px", minHeight: "calc(100vh - 68px - 300px)" }}>
            <Card styles={{ body: { padding: 0 } }} style={{ width: "100%", maxWidth: 940, borderRadius: 8, overflow: "hidden", boxShadow: "0 12px 32px rgba(0,0,0,.08)" }}>
                <Row>
                    <Col xs={0} md={12}>
                        <div
                            style={{
                                height: "100%", padding: "44px 36px", color: "#fff", display: "flex", flexDirection: "column", justifyContent: "center",
                                background: PRIMARY,
                            }}
                        >
                            <Title level={2} style={{ ...display, color: "#fff", margin: 0, fontSize: 34, lineHeight: 1.15, textTransform: "uppercase" }}>{side.title}</Title>
                            <Typography.Paragraph style={{ color: "#fff", opacity: 0.92, marginTop: 12, fontSize: 16 }}>{side.text}</Typography.Paragraph>
                        </div>
                    </Col>
                    <Col xs={24} md={12}>
                        <div style={{ padding: "40px 36px" }}>
                            <Title level={2} style={{ ...display, margin: 0 }}>{title}</Title>
                            <Text type="secondary" style={{ display: "block", margin: "4px 0 22px" }}>{subtitle}</Text>
                            {children}
                        </div>
                    </Col>
                </Row>
            </Card>
        </div>
    );
}

const LoginPage = ({ setView, onAuthSuccess, notice }) => {
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState("");

    const handleFinish = async (values) => {
        setLoading(true);
        setError("");
        try {
            const data = await authService.login(values);
            onAuthSuccess(data, "login");
        } catch (e) {
            setError(e.message);
        } finally {
            setLoading(false);
        }
    };

    return (
        <AuthShell
            title="Đăng nhập"
            subtitle="Nhập email và mật khẩu của bạn."
            side={{ title: "Chào mừng bạn quay lại", text: "Đăng nhập để theo dõi đơn hàng và đặt món nhanh hơn." }}
        >
            {notice && <Alert type="info" showIcon message={notice} style={{ marginBottom: 16 }} />}
            {error && <Alert type="error" showIcon message={error} style={{ marginBottom: 16 }} />}

            <Form layout="vertical" onFinish={handleFinish} requiredMark={false} size="large">
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
                    <Input.Password prefix={<LockOutlined />} placeholder="Ít nhất 6 ký tự" autoComplete="current-password" />
                </Form.Item>
                <Button type="primary" htmlType="submit" block loading={loading}>Đăng nhập</Button>
            </Form>

            {USE_MOCK && (
                <Text type="secondary" style={{ display: "block", textAlign: "center", marginTop: 14, fontSize: 13 }}>
                    Tài khoản thử: demo@foodie.vn / 123456
                </Text>
            )}
            <div style={{ textAlign: "center", marginTop: 16 }}>
                <Text type="secondary">Chưa có tài khoản? </Text>
                <Button type="link" style={{ padding: 0, fontWeight: 600 }} onClick={() => setView("register")}>Đăng ký</Button>
            </div>
        </AuthShell>
    );
}

export default LoginPage;
export { AuthShell };