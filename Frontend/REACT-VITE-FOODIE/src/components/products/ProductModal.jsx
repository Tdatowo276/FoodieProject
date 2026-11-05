import React, { useState } from 'react';
import { Modal, Row, Col, Tag, Typography, Rate, Space, InputNumber, Button } from 'antd';
import { ShoppingCartOutlined } from '@ant-design/icons';
import ProductImage from './ProductImage';
import { formatVND, PRIMARY, display } from '../../utils/helpers';

const { Title } = Typography;

const ProductModal = ({ product, onClose, onAdd }) => {
    const [qty, setQty] = useState(1);

    return (
        <Modal open={!!product} onCancel={onClose} footer={null} width={760} centered title={null}>
            {product && (
                <Row gutter={[24, 16]} align="middle" style={{ paddingTop: 12 }}>
                    <Col xs={24} md={12}>
                        <ProductImage product={product} height={280} radius={16} fontSize={130} />
                    </Col>
                    <Col xs={24} md={12}>
                        <Tag color="orange">{product.category}</Tag>
                        <Title level={2} style={{ ...display, margin: "8px 0" }}>{product.name}</Title>
                        <Rate disabled allowHalf value={product.rating} />
                        <Typography.Paragraph type="secondary" style={{ marginTop: 10 }}>{product.description}</Typography.Paragraph>
                        <Title level={3} style={{ ...display, color: PRIMARY, marginTop: 0 }}>{formatVND(product.price)}</Title>
                        <Space wrap>
                            <InputNumber min={1} max={99} value={qty} onChange={(v) => setQty(v || 1)} size="large" />
                            <Button
                                type="primary"
                                size="large"
                                icon={<ShoppingCartOutlined />}
                                onClick={() => {
                                    onAdd(product, qty);
                                    onClose();
                                    setQty(1);
                                }}
                            >
                                Thêm vào giỏ - {formatVND(product.price * qty)}
                            </Button>
                        </Space>
                    </Col>
                </Row>
            )}
        </Modal>
    );
}

export default ProductModal;