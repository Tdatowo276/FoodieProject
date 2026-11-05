import React, { useState, useEffect } from 'react';
import { Row, Col, Typography, Space, Button, Card, Flex, Avatar, Alert, Spin } from 'antd';
import { CarOutlined, ClockCircleOutlined, SafetyCertificateOutlined } from '@ant-design/icons';
import Container from '../components/Container';
import ProductGrid from '../components/products/ProductGrid';
import { productService } from '../services/product.service';
import { formatVND, FREE_SHIP_FROM, PRIMARY, display } from '../utils/helpers';

const { Title, Text } = Typography;

const HomePage = ({ setView, onAdd, onOpen }) => {
    const [featured, setFeatured] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    useEffect(() => {
        let ignore = false;
        productService
            .getFeatured()
            .then((data) => !ignore && setFeatured(data.slice(0, 8)))
            .catch((e) => !ignore && setError(e.message))
            .finally(() => !ignore && setLoading(false));
        return () => {
            ignore = true;
        };
    }, []);

    const perks = [
        { icon: <CarOutlined />, title: `Miễn phí giao từ ${formatVND(FREE_SHIP_FROM)}`, text: "Nội thành trong 30 phút" },
        { icon: <ClockCircleOutlined />, title: "Nấu mới khi có đơn", text: "Không hâm lại, không để sẵn" },
        { icon: <SafetyCertificateOutlined />, title: "Đổi món nếu chưa ưng", text: "Báo trong 15 phút sau khi nhận" },
    ];

    return (
        <>
            <section style={{ background: "linear-gradient(120deg, #E8321E 0%, #F2561C 55%, #FF8A00 100%)", color: "#fff", overflow: "hidden" }}>
                <Container style={{ padding: "56px 20px 80px" }}>
                    <Row gutter={[32, 24]} align="middle">
                        <Col xs={24} md={13}>
                            <Title level={1} style={{ ...display, color: "#fff", fontSize: "clamp(38px, 6vw, 66px)", lineHeight: 1.02, margin: "0 0 16px" }}>
                                Đói rồi?<br />Foodie lo hết.
                            </Title>
                            <Typography.Paragraph style={{ color: "#fff", fontSize: 18, maxWidth: 480, opacity: 0.95 }}>
                                Burger, cơm tấm, phở, trà đào... đặt trong 1 phút, nóng hổi đến cửa nhà bạn trong 30 phút.
                            </Typography.Paragraph>
                            <Space wrap size={12}>
                                <Button size="large" shape="round" onClick={() => setView("products")} style={{ color: "#BF2412", fontWeight: 600 }}>
                                    Xem thực đơn
                                </Button>
                                <Button size="large" shape="round" ghost onClick={() => setView("register")}>
                                    Tạo tài khoản
                                </Button>
                            </Space>
                        </Col>
                        <Col xs={0} md={11}>
                            <div
                                style={{
                                    margin: "0 auto", width: 300, height: 300, borderRadius: "50%", display: "grid", placeItems: "center",
                                    fontSize: 150, background: "rgba(255,255,255,.16)", border: "2px dashed rgba(255,255,255,.5)",
                                }}
                                aria-hidden="true"
                            >
                                🍔
                            </div>
                        </Col>
                    </Row>
                </Container>
            </section>

            <Container>
                <Row gutter={[16, 16]} style={{ marginTop: -36 }}>
                    {perks.map((p) => (
                        <Col key={p.title} xs={24} md={8}>
                            <Card styles={{ body: { padding: 18 } }} style={{ boxShadow: "0 10px 24px rgba(43,27,20,.08)" }}>
                                <Flex gap={14} align="center">
                                    <Avatar size={46} shape="square" icon={p.icon} style={{ background: "#FFF1DC", color: PRIMARY, borderRadius: 14, fontSize: 22 }} />
                                    <div>
                                        <Text strong style={{ display: "block" }}>{p.title}</Text>
                                        <Text type="secondary" style={{ fontSize: 13 }}>{p.text}</Text>
                                    </div>
                                </Flex>
                            </Card>
                        </Col>
                    ))}
                </Row>

                <section style={{ paddingTop: 56 }}>
                    <Flex justify="space-between" align="end" wrap gap={8} style={{ marginBottom: 24 }}>
                        <div>
                            <Title level={2} style={{ ...display, margin: 0 }}>Món được gọi nhiều nhất</Title>
                            <Text type="secondary">Khách quen của Foodie hay chọn những món này.</Text>
                        </div>
                        <Button type="link" onClick={() => setView("products")}>Xem toàn bộ thực đơn</Button>
                    </Flex>

                    {error ? (
                        <Alert type="error" showIcon message="Không tải được danh sách món" description={error} />
                    ) : (
                        <Spin spinning={loading}>
                            <div style={{ minHeight: 200 }}>
                                <ProductGrid products={featured} onAdd={onAdd} onOpen={onOpen} />
                            </div>
                        </Spin>
                    )}
                </section>

                <Card
                    style={{ marginTop: 64, background: "#2B1B14", border: "none", borderRadius: 28 }}
                    styles={{ body: { padding: 36 } }}
                >
                    <Flex justify="space-between" align="center" wrap gap={20}>
                        <div>
                            <Title level={3} style={{ ...display, color: "#fff", margin: 0 }}>Đơn đầu tiên giảm 20%</Title>
                            <Text style={{ color: "#fff", opacity: 0.8 }}>Đăng ký tài khoản để nhận mã ưu đãi cho lần đặt đầu tiên.</Text>
                        </div>
                        <Button type="primary" size="large" shape="round" onClick={() => setView("register")}>Đăng ký ngay</Button>
                    </Flex>
                </Card>
            </Container>
        </>
    );
}

export default HomePage;