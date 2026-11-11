import React from 'react';
import { Card, Flex, Typography, Button } from 'antd';
import ProductImage from './ProductImage';
import { formatVND } from '../../utils/helpers';

const { Text } = Typography;

/** Thẻ món: ảnh lớn, tên, giá, nút "Đặt món" viền đỏ. Bấm ảnh / tên để xem chi tiết. */
const ProductCard = ({ product, onAdd, onOpen }) => (
    <Card
        hoverable
        style={{ height: "100%", borderRadius: 6, border: "1px solid #F0F0F0", display: "flex", flexDirection: "column" }}
        styles={{ body: { padding: 14, display: "flex", flexDirection: "column", flex: 1 } }}
        cover={
            <button
                type="button"
                onClick={() => onOpen(product)}
                aria-label={`Xem chi tiết ${product.name}`}
                style={{ border: "none", padding: 12, background: "#fff", cursor: "pointer", borderRadius: "6px 6px 0 0" }}
            >
                <ProductImage product={product} height={200} />
            </button>
        }
    >
        <Text
            strong
            style={{ fontSize: 16, cursor: "pointer", flex: 1, marginBottom: 12 }}
            onClick={() => onOpen(product)}
        >
            {product.name}
        </Text>
        <Flex justify="space-between" align="center" gap={8} wrap>
            <Text strong style={{ fontSize: 16 }}>{formatVND(product.price)}</Text>
            <Button danger shape="round" onClick={() => onAdd(product)} aria-label={`Đặt ${product.name}`}>
                Đặt món
            </Button>
        </Flex>
    </Card>
);

export default ProductCard;
