import { useEffect, useState } from "react";
import {
    Alert,
    Breadcrumb,
    Button,
    Card,
    Col,
    Descriptions,
    Divider,
    InputNumber,
    Rate,
    Result,
    Row,
    Skeleton,
    Space,
    Tag,
    Typography,
} from "antd";
import { HomeOutlined, ShoppingCartOutlined } from "@ant-design/icons";

import { PRIMARY, display, formatVND } from "../utils/helpers";
import { productService } from "../services/product.service";
import Container from "../components/Container";
import ProductImage from "../components/products/ProductImage";

const { Title, Text, Paragraph, Link } = Typography;

/**
 * Lấy một món theo id.
 * Ưu tiên productService.getById (GET /api/products/{id}).
 * Nếu service chưa có hàm này, tạm lấy từ getAll() rồi tìm theo id.
 */
const loadProduct = async (id) => {
    if (typeof productService.getById === "function") {
        return productService.getById(id);
    }
    const list = await productService.getAll();
    return list.find((p) => String(p.id) === String(id)) ?? null;
};

/**
 * Props:
 * - productId:    id món cần xem
 * - onNavigate:   (key) => void, nhận "home" | "products" | "cart" ...
 * - onAddToCart:  (product, qty) => void
 */
export default function ProductDetail({ productId, onNavigate, onAddToCart }) {
    const [product, setProduct] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [qty, setQty] = useState(1);

    useEffect(() => {
        let ignore = false;
        setLoading(true);
        setError("");
        setProduct(null);
        setQty(1);

        loadProduct(productId)
            .then((data) => !ignore && setProduct(data))
            .catch((e) => !ignore && setError(e.message))
            .finally(() => !ignore && setLoading(false));

        return () => {
            ignore = true;
        };
    }, [productId]);

    const breadcrumbItems = [
        {
            title: (
                <Link onClick={() => onNavigate("home")}>
                    <HomeOutlined /> Trang chủ
                </Link>
            ),
        },
        { title: <Link onClick={() => onNavigate("products")}>Thực đơn</Link> },
        { title: product ? product.name : "Chi tiết món" },
    ];

    const renderBody = () => {
        if (loading) {
            return (
                <Card>
                    <Skeleton active avatar={{ shape: "square", size: 120 }} paragraph={{ rows: 6 }} />
                </Card>
            );
        }

        if (error) {
            return <Alert type="error" showIcon message="Không tải được thông tin món ăn" description={error} />;
        }

        if (!product) {
            return (
                <Result
                    status="404"
                    title="Không tìm thấy món ăn"
                    subTitle="Món này có thể đã ngừng bán hoặc đường dẫn chưa đúng."
                    extra={
                        <Button type="primary" onClick={() => onNavigate("products")}>
                            Xem thực đơn
                        </Button>
                    }
                />
            );
        }

        const rating = product.rating ?? 0;

        return (
            <Card styles={{ body: { padding: 28 } }} style={{ borderRadius: 8 }}>
                <Row gutter={[40, 24]}>
                    <Col xs={24} md={11}>
                        <ProductImage product={product} height={380} radius={20} fontSize={180} />
                    </Col>

                    <Col xs={24} md={13}>
                        <Tag color="orange">{product.category}</Tag>
                        <Title level={1} style={{ ...display, margin: "10px 0 6px", lineHeight: 1.1 }}>
                            {product.name}
                        </Title>

                        <Space size={8} align="center">
                            <Rate disabled allowHalf value={rating} />
                            <Text type="secondary">{rating.toFixed(1)} / 5</Text>
                        </Space>

                        <Title level={2} style={{ ...display, color: PRIMARY, margin: "16px 0 8px" }}>
                            {formatVND(product.price)}
                        </Title>

                        <Paragraph type="secondary" style={{ fontSize: 16 }}>
                            {product.description || "Mô tả món ăn đang được cập nhật."}
                        </Paragraph>

                        <Descriptions column={1} size="small" colon={false} labelStyle={{ width: 110, color: "#7A6A62" }}>
                            <Descriptions.Item label="Danh mục">{product.category}</Descriptions.Item>
                            <Descriptions.Item label="Giao hàng">Dự kiến trong 30 phút</Descriptions.Item>
                        </Descriptions>

                        <Divider />

                        <Space size={16} wrap align="center">
                            <Text strong>Số lượng</Text>
                            <InputNumber
                                size="large"
                                min={1}
                                max={99}
                                value={qty}
                                onChange={(v) => setQty(v || 1)}
                                aria-label="Số lượng"
                            />
                        </Space>

                        <Space size={12} wrap style={{ marginTop: 20, display: "flex" }}>
                            <Button
                                type="primary"
                                size="large"
                                icon={<ShoppingCartOutlined />}
                                onClick={() => onAddToCart?.(product, qty)}
                            >
                                Thêm vào giỏ - {formatVND(product.price * qty)}
                            </Button>
                            <Button size="large" onClick={() => onNavigate("products")}>
                                Tiếp tục xem thực đơn
                            </Button>
                        </Space>
                    </Col>
                </Row>
            </Card>
        );
    };

    return (
        <Container>
            <div style={{ padding: "28px 0 40px" }}>
                <Breadcrumb items={breadcrumbItems} style={{ marginBottom: 20 }} />
                {renderBody()}
            </div>
        </Container>
    );
}
