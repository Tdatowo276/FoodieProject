import React from 'react';
import { Row, Col } from 'antd';
import ProductCard from './ProductCard';
const ProductGrid = ({ products, onAdd, onOpen }) => {
    return (
        <Row gutter={[20, 20]}>
            {products.map((p) => (
                <Col key={p.id} xs={12} sm={12} md={8} lg={6}>
                    <ProductCard product={p} onAdd={onAdd} onOpen={onOpen} />
                </Col>
            ))}
        </Row>
    );
}
export default ProductGrid;