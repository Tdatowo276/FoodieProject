import React, { useState, useEffect } from 'react';
import { ConfigProvider, App as AntApp, Layout, Modal, Result, Button } from 'antd';
import viVN from 'antd/locale/vi_VN';

import { CART_KEY, USER_KEY, TOKEN_KEY, PRIMARY, formatVND, storage } from './utils/helpers';
import ProductsPage from './pages/ProductsPage';
import CartPage from './pages/CartPage';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import HomePage from './pages/HomePage';
import AppHeader from './components/common/AppHeader';
import AppFooter from './components/common/AppFooter';
import ProductModal from './components/products/ProductModal';
import CheckoutModal from './components/products/CheckoutModal';
import ProductDetail from './pages/ProductDetail';
import NotFoundPage from './pages/NotFoundPage';
import AdminDashboard from './pages/AdminDashboard';

const FoodieApp = () => {
  const { message } = AntApp.useApp();
  const [selectedProductId, setSelectedProductId] = useState(null);
  const [view, setView] = useState("home");
  const [cart, setCart] = useState(() => storage.get(CART_KEY, []));
  const [user, setUser] = useState(() => storage.get(USER_KEY, null));
  const [detail, setDetail] = useState(null);
  const [checkout, setCheckout] = useState(null); // { total, shipping } khi đang mở form thanh toán
  const [order, setOrder] = useState(null);
  const [loginNotice, setLoginNotice] = useState("");

  useEffect(() => storage.set(CART_KEY, cart), [cart]);
  useEffect(() => {
    if (user) storage.set(USER_KEY, user);
    else storage.remove(USER_KEY);
  }, [user]);

  const navigate = (next) => {
    if (next !== "login") setLoginNotice("");
    setView(next);
    window.scrollTo({ top: 0 });
  };

  const cartCount = cart.reduce((s, i) => s + i.qty, 0);

  /* ---- Giỏ hàng (lưu snapshot sản phẩm để không phụ thuộc danh sách đang lọc) ---- */
  const addToCart = (product, qty = 1) => {
    setCart((prev) => {
      const found = prev.find((i) => i.id === product.id);
      if (found) return prev.map((i) => (i.id === product.id ? { ...i, qty: i.qty + qty } : i));
      const { id, name, price, category, emoji, bg, imageUrl } = product;
      return [...prev, { id, name, price, category, emoji, bg, imageUrl, qty }];
    });
    message.success(`Đã thêm ${qty > 1 ? qty + " x " : ""}${product.name} vào giỏ`);
  };

  const setQty = (id, qty) => setCart((prev) => prev.map((i) => (i.id === id ? { ...i, qty } : i)));
  const removeItem = (id) => setCart((prev) => prev.filter((i) => i.id !== id));

  /* ---- Thanh toán ---- */
  const startCheckout = (summary) => {
    if (!user) {
      setLoginNotice("Hãy đăng nhập để hoàn tất thanh toán. Giỏ hàng của bạn vẫn được giữ lại.");
      setView("login");
      window.scrollTo({ top: 0 });
      return;
    }
    setCheckout(summary);
  };

  const handleOrderSuccess = (result) => {
    setCheckout(null);
    setCart([]);
    setOrder(result);
  };

  /* ---- Xác thực ---- */
  const handleAuthSuccess = ({ token, user: u }, mode) => {
    storage.set(TOKEN_KEY, token);
    setUser(u);
    message.success(mode === "register" ? "Tạo tài khoản thành công!" : `Xin chào ${u.name}!`);
    navigate(mode === "login" && cart.length > 0 ? "cart" : "home");
  };

  const handleLogout = () => {
    storage.remove(TOKEN_KEY);
    setUser(null);
    message.info("Bạn đã đăng xuất");
    navigate("home");
  };

  let page;
  switch (view) {
    case "products":
      page = (
        <ProductsPage onAdd={addToCart}
          onOpen={setDetail}
        />);
      break;
    case "product-detail":
      page = (
        <ProductDetail
          productId={selectedProductId}
          onNavigate={navigate}
          onAddToCart={addToCart}
        />
      );
      break;
    case "cart":
      page = (<CartPage cart={cart} s
        etView={navigate}
        onQty={setQty}
        onRemove={removeItem}
        onCheckout={startCheckout}
      />);
      break;
    case "admin":
      page = <AdminDashboard />;
      break;
    case "login":
      page = (<LoginPage setView={navigate}
        onAuthSuccess={handleAuthSuccess}
        notice={loginNotice}
      />);
      break;
    case "register":
      page = (<RegisterPage setView={navigate}
        onAuthSuccess={handleAuthSuccess}
      />);
      break;
    case "404":
      page = <NotFoundPage onNavigate={navigate} />;
      break;
    default:
      page = (<HomePage setView={navigate}
        onAdd={addToCart}
        onOpen={setDetail}
      />);
  }

  return (
    <Layout style={{ minHeight: "100vh", background: "#FFFDFA" }}>
      <AppHeader view={view} setView={navigate} cartCount={cartCount} user={user} onLogout={handleLogout} />
      <Layout.Content style={{ paddingBottom: 40 }}>{page}</Layout.Content>
      <AppFooter setView={navigate} />

      <ProductModal product={detail} onClose={() => setDetail(null)} onAdd={addToCart} />

      <CheckoutModal
        open={!!checkout}
        onClose={() => setCheckout(null)}
        cart={cart}
        total={checkout?.total ?? 0}
        shipping={checkout?.shipping ?? 0}
        user={user}
        onSuccess={handleOrderSuccess}
        notify={message}
      />

      <Modal open={!!order} footer={null} onCancel={() => setOrder(null)} centered width={480}>
        <Result
          status="success"
          title="Đặt hàng thành công"
          subTitle={
            order && (
              <>
                Mã đơn: <b>{order.orderCode}</b> · Tổng thanh toán: <b>{formatVND(order.total)}</b>
                <br />
                Bếp đang chuẩn bị món cho bạn, dự kiến giao trong 30 phút.
              </>
            )
          }
          extra={
            <Button
              type="primary"
              size="large"
              onClick={() => {
                setOrder(null);
                navigate("home");
              }}
            >
              Về trang chủ
            </Button>
          }
        />
      </Modal>
    </Layout>
  );
}

export default function App() {
  return (
    <ConfigProvider
      locale={viVN}
      theme={{
        token: {
          colorPrimary: PRIMARY,
          colorInfo: PRIMARY,
          borderRadius: 12,
          fontFamily: "'Be Vietnam Pro', system-ui, sans-serif",
          colorTextBase: "#2B1B14",
        },
      }}
    >
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=Baloo+2:wght@600;700;800&family=Be+Vietnam+Pro:wght@400;500;600;700&display=swap');
        body { margin: 0; background: #FFFDFA; }
      `}</style>
      <AntApp>
        <FoodieApp />
      </AntApp>
    </ConfigProvider>
  );
}
