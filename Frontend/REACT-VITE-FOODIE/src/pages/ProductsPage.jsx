import React, { useState, useEffect } from 'react';
import { Typography, Flex, Input, Tag, Alert, Spin, Empty, Button } from 'antd';
import { SearchOutlined } from '@ant-design/icons';
import Container from '../components/Container';
import ProductGrid from '../components/products/ProductGrid';
import { productService } from '../services/product.service';
import { CATEGORIES, display } from '../utils/helpers';

const { Title, Text } = Typography;

const ProductsPage = ({ onAdd, onOpen }) => {
    const [keyword, setKeyword] = useState("");
    const [category, setCategory] = useState("Tất cả");
    const [products, setProducts] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    // Gọi API mỗi khi đổi từ khóa / danh mục (debounce 300ms cho ô tìm kiếm)
    useEffect(() => {
        let ignore = false;
        setLoading(true);
        const timer = setTimeout(async () => {
            try {
                const data = await productService.getAll({
                    keyword: keyword.trim() || undefined,
                    category: category === "Tất cả" ? undefined : category,
                });
                if (!ignore) {
                    setProducts(data);
                    setError("");
                }
            } catch (e) {
                if (!ignore) setError(e.message);
            } finally {
                if (!ignore) setLoading(false);
            }
        }, 300);
        return () => {
            ignore = true;
            clearTimeout(timer);
        };
    }, [keyword, category]);

    const reset = () => {
        setKeyword("");
        setCategory("Tất cả");
    };

    return (
        <Container style={{ paddingBottom: 24 }}>
            <div style={{ padding: "36px 0 20px" }}>
                <Title level={1} style={{ ...display, margin: 0 }}>Thực đơn hôm nay</Title>
                <Text type="secondary">Tìm theo tên món hoặc lọc theo nhóm để chọn nhanh hơn.</Text>
            </div>

            <Flex vertical gap={16} style={{ marginBottom: 24 }}>
                <Input
                    allowClear
                    size="large"
                    prefix={<SearchOutlined />}
                    placeholder="Tìm món: phở, burger, trà đào..."
                    value={keyword}
                    onChange={(e) => setKeyword(e.target.value)}
                    style={{ maxWidth: 520, borderRadius: 999 }}
                />
                <Flex wrap gap={8} role="group" aria-label="Lọc theo danh mục">
                    {CATEGORIES.map((c) => (
                        <Tag.CheckableTag
                            key={c}
                            checked={category === c}
                            onChange={() => setCategory(c)}
                            style={{ padding: "6px 16px", fontSize: 14, borderRadius: 999, border: "1px solid #F0E3D8" }}
                        >
                            {c}
                        </Tag.CheckableTag>
                    ))}
                </Flex>
            </Flex>

            {error ? (
                <Alert type="error" showIcon message="Không tải được thực đơn" description={error} />
            ) : (
                <Spin spinning={loading}>
                    <div style={{ minHeight: 260 }}>
                        {!loading && products.length === 0 ? (
                            <Empty description="Không có món nào khớp. Thử đổi từ khóa hoặc nhóm món khác." style={{ padding: "48px 0" }}>
                                <Button type="primary" onClick={reset}>Xóa bộ lọc</Button>
                            </Empty>
                        ) : (
                            <>
                                <Text type="secondary" style={{ display: "block", marginBottom: 14 }}>
                                    Tìm thấy {products.length} món
                                </Text>
                                <ProductGrid products={products} onAdd={onAdd} onOpen={onOpen} />
                            </>
                        )}
                    </div>
                </Spin>
            )}
        </Container>
    );
}

export default ProductsPage;