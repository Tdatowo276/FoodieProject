import React from 'react';
import { Card, Tag, Rate, Flex, Typography, Button } from 'antd';
import { FireFilled, PlusOutlined } from '@ant-design/icons';
import ProductImage from './ProductImage';
import { formatVND, PRIMARY, display } from '../../utils/helpers';

const { Title, Text } = Typography;

const ProductCard = ({ product, onAdd, onOpen }) => {
    return (
        <Card
            hoverable
            style={{ height: "100%", overflow: "hidden" }}
            styles={{ body: { padding: 16 } }}
            cover={
                <div onClick={() => onOpen(product)} style={{ position: "relative", cursor: "pointer" }}>
                    <ProductImage product={product} height={170} />
                    {product.isFeatured && (
                        <Tag color="orange" icon={<FireFilled />} style={{ position: "absolute", top: 10, left: 10, margin: 0 }}>
                            Bán chạy
                        </Tag>
                    )}
                </div>
            }
        >
            <Text type="secondary" style={{ fontSize: 13 }}>{product.category}</Text>
            <Title
                level={5}
                style={{ margin: "2px 0 6px", cursor: "pointer" }}
                onClick={() => onOpen(product)}
            >
                {product.name}
            </Title>
            <Rate disabled allowHalf value={product.rating} style={{ fontSize: 13 }} />
            <Flex justify="space-between" align="center" style={{ marginTop: 12 }} gap={8}>
                <Text strong style={{ ...display, fontSize: 20, color: PRIMARY }}>{formatVND(product.price)}</Text>
                <Button type="primary" shape="round" icon={<PlusOutlined />} onClick={() => onAdd(product)} aria-label={`Thêm ${product.name} vào giỏ`}>
                    Thêm
                </Button>
            </Flex>
        </Card>
    );
}

export default ProductCard;