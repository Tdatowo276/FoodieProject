import { Button, Result, Space } from "antd";
import { HomeOutlined, ShopOutlined } from "@ant-design/icons";

import { PRIMARY, display } from "../utils/helpers";
import Container from "../components/Container";

/**
 * Props:
 * - onNavigate: (key) => void, nhận "home" | "products" ...
 */
export default function NotFoundPage({ onNavigate }) {
    return (
        <Container>
            <div style={{ padding: "64px 0" }}>
                <Result
                    status="404"
                    title={<span style={{ ...display, fontSize: 56, color: PRIMARY }}>404</span>}
                    subTitle="Trang bạn tìm không tồn tại hoặc đã được chuyển sang địa chỉ khác."
                    extra={
                        <Space wrap size={12}>
                            <Button type="primary" size="large" icon={<HomeOutlined />} onClick={() => onNavigate("home")}>
                                Về trang chủ
                            </Button>
                            <Button size="large" icon={<ShopOutlined />} onClick={() => onNavigate("products")}>
                                Xem thực đơn
                            </Button>
                        </Space>
                    }
                />
            </div>
        </Container>
    );
}
