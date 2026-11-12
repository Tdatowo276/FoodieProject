import React from 'react';
import { Layout, Row, Col, Typography, Flex, Button } from 'antd';
import { EnvironmentOutlined, PhoneOutlined, MailOutlined, ExpandOutlined } from '@ant-design/icons';
import Container from '../Container';
import ShopMap from '../contact/ShopMap';
import { useContact } from '../contact/contactContext';
import contact from '../../config/contact';
import { display } from '../../utils/helpers';

const { Title, Text } = Typography;

const AppFooter = ({ setView }) => {
    const { openMap, openPhone, openFeedback } = useContact();
    const linkStyle = { color: "#D9D9D9", padding: 0, height: "auto" };
    return (
        <Layout.Footer style={{ background: "#1F1F1F", color: "#D9D9D9", padding: 0, marginTop: 40 }}>
            <Container style={{ padding: "48px 20px 28px" }}>
                <Row gutter={[32, 28]}>
                    <Col xs={24} md={7}>
                        <div style={{ ...display, fontSize: 28, color: "#fff", marginBottom: 8 }}>FOODIE</div>
                        <Text style={{ color: "#D9D9D9", opacity: 0.8 }}>
                            Đồ ăn nóng hổi, giao tận nhà trong 30 phút. Nấu mới mỗi ngày từ nguyên liệu tươi.
                        </Text>
                    </Col>
                    <Col xs={12} md={4}>
                        <Title level={5} style={{ color: "#fff" }}>Khám phá</Title>
                        <Flex vertical gap={8} align="flex-start">
                            <Button type="link" style={linkStyle} onClick={() => setView("home")}>Trang chủ</Button>
                            <Button type="link" style={linkStyle} onClick={() => setView("products")}>Thực đơn</Button>
                            <Button type="link" style={linkStyle} onClick={() => setView("about")}>Về chúng tôi</Button>
                            <Button type="link" style={linkStyle} onClick={() => setView("cart")}>Giỏ hàng</Button>
                        </Flex>
                    </Col>
                    <Col xs={12} md={4}>
                        <Title level={5} style={{ color: "#fff" }}>Hỗ trợ</Title>
                        <Flex vertical gap={8} align="flex-start">
                            <Button type="link" style={linkStyle} onClick={openFeedback}>Gửi góp ý</Button>
                            <Button type="link" style={linkStyle} onClick={openPhone}>Gọi điện</Button>
                            <Button type="link" style={linkStyle} onClick={openMap}>Vị trí cửa hàng</Button>
                        </Flex>
                    </Col>
                    <Col xs={24} md={9}>
                        <Title level={5} style={{ color: "#fff" }}>Liên hệ</Title>
                        <Flex vertical gap={8} style={{ marginBottom: 14 }}>
                            <Text style={{ color: "#D9D9D9" }}><EnvironmentOutlined /> {contact.address}</Text>
                            <Text style={{ color: "#D9D9D9" }}><PhoneOutlined /> {contact.phoneDisplay}</Text>
                            <Text style={{ color: "#D9D9D9" }}><MailOutlined /> {contact.email}</Text>
                        </Flex>
                        {/* Bản đồ thu nhỏ: bấm vào để mở bản đồ lớn có biểu tượng ngôi nhà */}
                        <button
                            type="button"
                            onClick={openMap}
                            aria-label="Mở bản đồ vị trí cửa hàng"
                            style={{ display: "block", width: "100%", padding: 0, border: "none", background: "none", cursor: "pointer", position: "relative" }}
                        >
                            <ShopMap height={170} zoom={15} interactive={false} />
                            <span
                                style={{
                                    position: "absolute", right: 8, bottom: 8, zIndex: 1, background: "rgba(0,0,0,.65)", color: "#fff",
                                    fontSize: 12, padding: "4px 10px", borderRadius: 999,
                                }}
                            >
                                <ExpandOutlined /> Mở bản đồ
                            </span>
                        </button>
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