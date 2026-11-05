import React from 'react';
import { Layout, Menu, Badge, Button, Dropdown, Space, Avatar, Typography, Grid } from 'antd';
import { HomeOutlined, ShopOutlined, ShoppingCartOutlined, UserOutlined, LogoutOutlined } from '@ant-design/icons';
import { PRIMARY, display } from '../../utils/helpers';

const { Text } = Typography;
const { useBreakpoint } = Grid;

const AppHeader = ({ view, setView, cartCount, user, onLogout }) => {
    const screens = useBreakpoint();
    const isMobile = !screens.md;

    const menuItems = [
        { key: "home", icon: <HomeOutlined />, label: isMobile ? null : "Trang chủ", title: "Trang chủ" },
        { key: "products", icon: <ShopOutlined />, label: isMobile ? null : "Thực đơn", title: "Thực đơn" },
    ];

    return (
        <Layout.Header
            style={{
                position: "sticky",
                top: 0,
                zIndex: 50,
                display: "flex",
                alignItems: "center",
                gap: 12,
                background: "#fff",
                padding: isMobile ? "0 12px" : "0 28px",
                borderBottom: "1px solid #F0E3D8",
                height: 68,
                lineHeight: "68px",
            }}
        >
            <div
                onClick={() => setView("home")}
                style={{ ...display, fontSize: 28, color: PRIMARY, cursor: "pointer", display: "flex", alignItems: "center", gap: 8 }}
            >
                <span
                    style={{
                        width: 36, height: 36, borderRadius: 11, display: "grid", placeItems: "center", fontSize: 20,
                        background: "linear-gradient(135deg, #FF8A00, #E8321E)",
                    }}
                >
                    🍴
                </span>
                {!isMobile && "Foodie"}
            </div>

            <Menu
                mode="horizontal"
                selectedKeys={[view]}
                items={menuItems}
                onClick={({ key }) => setView(key)}
                style={{ flex: 1, minWidth: 0, borderBottom: "none", background: "transparent" }}
            />

            <Space size={12}>
                <Badge count={cartCount} size="small" color={PRIMARY}>
                    <Button
                        shape="circle"
                        size="large"
                        type={view === "cart" ? "primary" : "default"}
                        icon={<ShoppingCartOutlined />}
                        onClick={() => setView("cart")}
                        aria-label={`Giỏ hàng, ${cartCount} món`}
                    />
                </Badge>

                {user ? (
                    <Dropdown
                        menu={{
                            items: [
                                { key: "email", label: user.email, disabled: true },
                                { key: "logout", icon: <LogoutOutlined />, label: "Đăng xuất", onClick: onLogout },
                            ],
                        }}
                        placement="bottomRight"
                    >
                        <Space style={{ cursor: "pointer" }}>
                            <Avatar style={{ background: PRIMARY }}>{user.name.trim().charAt(0).toUpperCase()}</Avatar>
                            {!isMobile && <Text strong>{user.name}</Text>}
                        </Space>
                    </Dropdown>
                ) : (
                    <Button type="primary" icon={<UserOutlined />} onClick={() => setView("login")}>
                        {isMobile ? null : "Đăng nhập"}
                    </Button>
                )}
            </Space>
        </Layout.Header>
    );
}

export default AppHeader;