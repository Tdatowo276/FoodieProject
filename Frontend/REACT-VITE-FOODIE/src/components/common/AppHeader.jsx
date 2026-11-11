import React from 'react';
import { Layout, Menu, Badge, Button, Dropdown, Space, Avatar, Typography, Grid } from 'antd';
import {
    HomeOutlined, ShopOutlined, ShoppingCartOutlined, UserOutlined, LogoutOutlined, ProfileOutlined, SettingOutlined,
} from '@ant-design/icons';
import { PRIMARY, display, ADMIN_URL, resolveImageUrl } from '../../utils/helpers';

const { Text } = Typography;
const { useBreakpoint } = Grid;

const AppHeader = ({ view, setView, cartCount, user, onLogout }) => {
    const screens = useBreakpoint();
    const isMobile = !screens.md;

    const menuItems = [
        { key: "home", icon: isMobile ? <HomeOutlined /> : null, label: isMobile ? null : "TRANG CHỦ", title: "Trang chủ" },
        { key: "products", icon: isMobile ? <ShopOutlined /> : null, label: isMobile ? null : "THỰC ĐƠN", title: "Thực đơn" },
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
                padding: isMobile ? "0 12px" : "0 max(28px, calc((100% - 1180px) / 2))",
                borderTop: `6px solid ${PRIMARY}`,
                borderBottom: "1px solid #EEE",
                height: 68,
                lineHeight: "62px",
            }}
        >
            <button
                type="button"
                onClick={() => setView("home")}
                aria-label="Foodie - về trang chủ"
                style={{
                    ...display, fontSize: 26, color: PRIMARY, cursor: "pointer", background: "none", border: "none",
                    padding: 0, display: "flex", alignItems: "center", gap: 8, letterSpacing: "0.02em",
                }}
            >
                <span
                    aria-hidden="true"
                    style={{
                        width: 34, height: 34, borderRadius: 6, background: PRIMARY, color: "#fff",
                        display: "grid", placeItems: "center", fontSize: 20, lineHeight: 1,
                    }}
                >
                    F
                </span>
                {!isMobile && "FOODIE"}
            </button>

            <Menu
                mode="horizontal"
                selectedKeys={[view]}
                items={menuItems}
                onClick={({ key }) => setView(key)}
                style={{
                    flex: 1, minWidth: 0, borderBottom: "none", background: "transparent",
                    justifyContent: isMobile ? "flex-start" : "center", fontWeight: 600,
                }}
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
                                { type: "divider" },
                                { key: "profile", icon: <UserOutlined />, label: "Tài khoản của tôi", onClick: () => setView("profile") },
                                { key: "orders", icon: <ProfileOutlined />, label: "Đơn của tôi", onClick: () => setView("orders") },
                                // Trang quản trị là ASP.NET Core MVC trên backend (đăng nhập riêng bằng cookie).
                                // Đây chỉ là đường dẫn; quyền admin luôn được kiểm tra phía server.
                                ...(user.role === "Admin"
                                    ? [{
                                        key: "admin",
                                        icon: <SettingOutlined />,
                                        label: <a href={ADMIN_URL} target="_blank" rel="noopener noreferrer">Trang quản trị</a>,
                                    }]
                                    : []),
                                { type: "divider" },
                                { key: "logout", icon: <LogoutOutlined />, label: "Đăng xuất", onClick: onLogout },
                            ],
                        }}
                        placement="bottomRight"
                    >
                        <Space style={{ cursor: "pointer" }}>
                            <Avatar src={resolveImageUrl(user.avatarUrl)} style={{ background: user.avatarUrl ? "#fff" : PRIMARY }}>
                                {user.name.trim().charAt(0).toUpperCase()}
                            </Avatar>
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