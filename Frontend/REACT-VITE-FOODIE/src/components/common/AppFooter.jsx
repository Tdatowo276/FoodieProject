import React from 'react';
import { Layout, Row, Col, Typography, Flex, Button } from 'antd';
import { EnvironmentOutlined, PhoneOutlined, MailOutlined } from '@ant-design/icons';
import Container from '../Container';
import { display } from '../../utils/helpers';

const { Title, Text } = Typography;

const AppFooter = ({ setView }) => {
    const linkStyle = { color: "#D9D9D9", padding: 0, height: "auto" };
    return (
        <Layout.Footer style={{ background: "#1F1F1F", color: "#D9D9D9", padding: 0, marginTop: 40 }}>
            <Container style={{ padding: "48px 20px 28px" }}>
                <Row gutter={[32, 28]}>
                    <Col xs={24} md={9}>
                        <div style={{ ...display, fontSize: 28, color: "#fff", marginBottom: 8 }}>FOODIE</div>
                        <Text style={{ color: "#D9D9D9", opacity: 0.8 }}>
                            Đồ ăn nóng hổi, giao tận nhà trong 30 phút. Nấu mới mỗi ngày từ nguyên liệu tươi.
                        </Text>
                    </Col>
                    <Col xs={12} md={5}>
                        <Title level={5} style={{ color: "#fff" }}>Khám phá</Title>
                        <Flex vertical gap={8} align="flex-start">
                            <Button type="link" style={linkStyle} onClick={() => setView("home")}>Trang chủ</Button>
                            <Button type="link" style={linkStyle} onClick={() => setView("products")}>Thực đơn</Button>
                            <Button type="link" style={linkStyle} onClick={() => setView("about")}>Về chúng tôi</Button>
                            <Button type="link" style={linkStyle} onClick={() => setView("cart")}>Giỏ hàng</Button>
                        </Flex>
                    </Col>
                    <Col xs={12} md={5}>
                        <Title level={5} style={{ color: "#fff" }}>Tài khoản</Title>
                        <Flex vertical gap={8} align="flex-start">
                            <Button type="link" style={linkStyle} onClick={() => setView("login")}>Đăng nhập</Button>
                            <Button type="link" style={linkStyle} onClick={() => setView("register")}>Đăng ký</Button>
                        </Flex>
                    </Col>
                    <Col xs={24} md={5}>
                        <Title level={5} style={{ color: "#fff" }}>Liên hệ</Title>
                        <Flex vertical gap={8}>
                            <Text style={{ color: "#D9D9D9", opacity: 0.8 }}><EnvironmentOutlined /> 54 Ngo 10 Lang Ha, Ha Noi</Text>
                            <Text style={{ color: "#D9D9D9", opacity: 0.8 }}><PhoneOutlined /> +84 974 679 274</Text>
                            <Text style={{ color: "#D9D9D9", opacity: 0.8 }}><MailOutlined /> WupyTdat@gmail.com</Text>
                        </Flex>
                    </Col>
                </Row>
            </Container>
            <div style={{ borderTop: "1px solid rgba(255,255,255,.12)", textAlign: "center", padding: "16px 20px", fontSize: 13, opacity: 0.65 }}>
                © {new Date().getFullYear()} Foodie. Dự án học tập.
            </div>
        </Layout.Footer>
    );
}

export default AppFooter;