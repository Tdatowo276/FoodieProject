import React from 'react';
import { Typography, Empty, Button, Row, Col, Card, Table, Flex, InputNumber, Popconfirm, Alert, Progress } from 'antd';
import { DeleteOutlined } from '@ant-design/icons';
import Container from '../components/Container';
import ProductImage from '../components/products/ProductImage';
import { formatVND, FREE_SHIP_FROM, SHIPPING_FEE, PRIMARY, display } from '../utils/helpers';

const { Title, Text } = Typography;


const CartPage = ({ cart, setView, onQty, onRemove, onCheckout }) => {
    const subtotal = cart.reduce((s, i) => s + i.price * i.qty, 0);
    const shipping = cart.length === 0 || subtotal >= FREE_SHIP_FROM ? 0 : SHIPPING_FEE;
    const total = subtotal + shipping;
    const progress = Math.min(100, Math.round((subtotal / FREE_SHIP_FROM) * 100));

    if (cart.length === 0) {
        return (
            <Container>
                <div style={{ padding: "36px 0 20px" }}>
                    <Title level={1} style={{ ...display, margin: 0 }}>Giỏ hàng</Title>
                </div>
                <Empty description="Giỏ hàng đang trống. Chọn vài món ngon từ thực đơn nhé." style={{ padding: "48px 0" }}>
                    <Button type="primary" size="large" onClick={() => setView("products")}>Xem thực đơn</Button>
                </Empty>
            </Container>
        );
    }

    const columns = [
        {
            title: "Món",
            key: "item",
            render: (_, item) => (
                <Flex gap={12} align="center">
                    <div style={{ width: 64, flexShrink: 0 }}>
                        <ProductImage product={item} height={64} radius={12} fontSize={34} />
                    </div>
                    <div>
                        <Text strong style={{ display: "block" }}>{item.name}</Text>
                        <Text type="secondary">{formatVND(item.price)} / phần</Text>
                    </div>
                </Flex>
            ),
        },
        {
            title: "Số lượng",
            key: "qty",
            width: 120,
            render: (_, item) => (
                <InputNumber min={1} max={99} value={item.qty} onChange={(v) => onQty(item.id, v || 1)} />
            ),
        },
        {
            title: "Thành tiền",
            key: "total",
            width: 130,
            align: "right",
            render: (_, item) => <Text strong style={{ color: PRIMARY }}>{formatVND(item.price * item.qty)}</Text>,
        },
        {
            key: "action",
            width: 56,
            render: (_, item) => (
                <Popconfirm title={`Xóa ${item.name}?`} okText="Xóa" cancelText="Giữ lại" onConfirm={() => onRemove(item.id)}>
                    <Button type="text" danger icon={<DeleteOutlined />} aria-label={`Xóa ${item.name}`} />
                </Popconfirm>
            ),
        },
    ];

    return (
        <Container>
            <div style={{ padding: "36px 0 20px" }}>
                <Title level={1} style={{ ...display, margin: 0 }}>Giỏ hàng</Title>
                <Text type="secondary">{cart.reduce((s, i) => s + i.qty, 0)} món đang chờ bạn.</Text>
            </div>

            <Row gutter={[24, 24]} align="top">
                <Col xs={24} lg={16}>
                    <Card styles={{ body: { padding: 0 } }}>
                        <Table rowKey="id" columns={columns} dataSource={cart} pagination={false} scroll={{ x: 560 }} />
                    </Card>
                </Col>

                <Col xs={24} lg={8}>
                    <Card title={<span style={{ ...display, fontSize: 20 }}>Tóm tắt đơn hàng</span>} style={{ position: "sticky", top: 88 }}>
                        <Alert
                            type={shipping === 0 ? "success" : "warning"}
                            showIcon
                            message={
                                shipping === 0
                                    ? "Đơn của bạn được miễn phí giao hàng."
                                    : `Mua thêm ${formatVND(FREE_SHIP_FROM - subtotal)} để được miễn phí giao hàng.`
                            }
                            style={{ marginBottom: 12 }}
                        />
                        <Progress percent={progress} showInfo={false} strokeColor={{ from: "#FF8A00", to: "#E8321E" }} />

                        <Flex justify="space-between" style={{ marginTop: 14 }}>
                            <Text type="secondary">Tạm tính</Text>
                            <Text strong>{formatVND(subtotal)}</Text>
                        </Flex>
                        <Flex justify="space-between" style={{ marginTop: 8 }}>
                            <Text type="secondary">Phí giao hàng</Text>
                            <Text strong>{shipping === 0 ? "Miễn phí" : formatVND(shipping)}</Text>
                        </Flex>
                        <Flex
                            justify="space-between"
                            align="baseline"
                            style={{ margin: "16px 0", paddingTop: 14, borderTop: "2px dashed #F0E3D8" }}
                        >
                            <Text strong>Tổng cộng</Text>
                            <Text style={{ ...display, fontSize: 30, color: PRIMARY }}>{formatVND(total)}</Text>
                        </Flex>

                        <Button type="primary" size="large" block onClick={() => onCheckout({ total, shipping })}>
                            Thanh toán
                        </Button>
                        <Button size="large" block style={{ marginTop: 10 }} onClick={() => setView("products")}>
                            Chọn thêm món
                        </Button>
                    </Card>
                </Col>
            </Row>
        </Container>
    );
}

export default CartPage;