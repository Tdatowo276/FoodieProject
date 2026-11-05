import { useCallback, useEffect, useState } from "react";
import { Alert, Button, Card, Col, Flex, Row, Tabs, Typography } from "antd";
import {
    DollarOutlined,
    ReloadOutlined,
    ShoppingOutlined,
    TeamOutlined,
    UnorderedListOutlined,
} from "@ant-design/icons";

import { display, formatVND } from "../utils/helpers";
import { adminService } from "../services/admin.service";
import Container from "../components/Container";
import StatCard from "../components/admin/StatCard";
import OrdersTable from "../components/admin/OrdersTable";
import ProductsManager from "../components/admin/ProductsManager";

const { Title, Text } = Typography;

const TAB_ITEMS = [
    {
        key: "orders",
        label: (
            <span>
                <ShoppingOutlined /> Đơn hàng
            </span>
        ),
        children: <OrdersTable />,
    },
    {
        key: "menu",
        label: (
            <span>
                <UnorderedListOutlined /> Quản lý thực đơn
            </span>
        ),
        children: <ProductsManager />,
    },
];

export default function AdminDashboard() {
    const [stats, setStats] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    const loadStats = useCallback(async () => {
        setLoading(true);
        setError("");
        try {
            setStats(await adminService.getStats());
        } catch (e) {
            setError(e.message);
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        loadStats();
    }, [loadStats]);

    const initialLoading = loading && !stats;

    return (
        <Container>
            <div style={{ padding: "32px 0 48px" }}>
                <Flex justify="space-between" align="center" wrap gap={12} style={{ marginBottom: 24 }}>
                    <div>
                        <Title level={1} style={{ ...display, margin: 0 }}>Quản trị Foodie</Title>
                        <Text type="secondary">Theo dõi đơn hàng và quản lý thực đơn.</Text>
                    </div>
                    <Button icon={<ReloadOutlined />} onClick={loadStats} loading={loading}>
                        Làm mới số liệu
                    </Button>
                </Flex>

                {error && (
                    <Alert
                        type="error"
                        showIcon
                        message="Không tải được số liệu thống kê"
                        description={error}
                        style={{ marginBottom: 20 }}
                    />
                )}

                <Row gutter={[20, 20]}>
                    <Col xs={24} md={8}>
                        <StatCard
                            title="Doanh thu"
                            value={stats?.revenue.value ?? 0}
                            growth={stats?.revenue.growth ?? 0}
                            icon={<DollarOutlined />}
                            format={formatVND}
                            loading={initialLoading}
                        />
                    </Col>
                    <Col xs={24} md={8}>
                        <StatCard
                            title="Đơn hàng"
                            value={stats?.orders.value ?? 0}
                            growth={stats?.orders.growth ?? 0}
                            icon={<ShoppingOutlined />}
                            loading={initialLoading}
                        />
                    </Col>
                    <Col xs={24} md={8}>
                        <StatCard
                            title="Khách hàng"
                            value={stats?.customers.value ?? 0}
                            growth={stats?.customers.growth ?? 0}
                            icon={<TeamOutlined />}
                            loading={initialLoading}
                        />
                    </Col>
                </Row>

                <Card style={{ marginTop: 24 }}>
                    <Tabs defaultActiveKey="orders" items={TAB_ITEMS} />
                </Card>
            </div>
        </Container>
    );
}
