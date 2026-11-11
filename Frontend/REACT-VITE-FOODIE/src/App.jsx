import React, { useState, useEffect } from 'react';
import { ConfigProvider, App as AntApp, Layout, Modal, Result, Button } from 'antd';
import viVN from 'antd/locale/vi_VN';

import { CART_KEY, PRIMARY, formatVND, storage } from './utils/helpers';
import { useAuthView } from './hooks/useAuthView';

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
import MyOrdersPage from './pages/MyOrdersPage';

const FoodieApp = () => {
  const { message } = AntApp.useApp();
  const [selectedProductId, setSelectedProductId] = useState(null);
  const [cart, setCart] = useState(() => storage.get(CART_KEY, []));
  const [detail, setDetail] = useState(null);
  const [checkout, setCheckout] = useState(null);
  const [order, setOrder] = useState(null);
  const [openOrderCode, setOpenOrderCode] = useState(null);   // mở sẵn chi tiết đơn khi vào "Đơn của tôi"
  const [searchKeyword, setSearchKeyword] = useState('');     // từ khóa nhập ở trang chủ => trang Thực đơn
  const {
    view,
    user,
    loginNotice,
    setLoginNotice,
    navigate,
    handleAuthSuccess,
    handleLogout,
  } = useAuthView(cart.length, message);

  useEffect(() => storage.set(CART_KEY, cart), [cart]);

  const cartCount = cart.reduce((s, i) => s + i.qty, 0);

  /* ---- Giỏ hàng ---- */
  const addToCart = (product, qty = 1) => {
    setCart((prev) => {
      const found = prev.find((i) => i.id === product.id);
      if (found) return prev.map((i) => (i.id === product.id ? { ...i, qty: i.qty + qty } : i));
      const { id, name, price, category, emoji, bg, imageUrl } = product;
      return [...prev, { id, name, price, category, emoji, bg, imageUrl, qty }];
    });
    message.success(`Đã thêm ${qty > 1 ? qty + ' x ' : ''}${product.name} vào giỏ`);
  };

  const setQty = (id, qty) => setCart((prev) => prev.map((i) => (i.id === id ? { ...i, qty } : i)));
  const removeItem = (id) => setCart((prev) => prev.filter((i) => i.id !== id));

  /* ---- Thanh toán ---- */
  const startCheckout = (summary) => {
    if (!user) {
      setLoginNotice('Hãy đăng nhập để hoàn tất thanh toán. Giỏ hàng của bạn vẫn được giữ lại.');
      navigate('login');
      return;
    }
    setCheckout(summary);
  };

  const handleOrderSuccess = (result) => {
    setCheckout(null);
    setCart([]);
    setOrder(result);
  };

  let page;
  switch (view) {
    case 'products':
      page = <ProductsPage key={searchKeyword} initialKeyword={searchKeyword} onAdd={addToCart} onOpen={setDetail} />;
      break;
    case 'product-detail':
      page = (
        <ProductDetail
          productId={selectedProductId}
          onNavigate={navigate}
          onAddToCart={addToCart}
        />
      );
      break;
    case 'cart':
      page = (
        <CartPage
          cart={cart}
          setView={navigate}
          onQty={setQty}
          onRemove={removeItem}
          onCheckout={startCheckout}
        />
      );
      break;
    case 'orders':
      // Trang cần đăng nhập; phiên hết hạn thì useAuthView tự chuyển về trang đăng nhập
      page = user
        ? <MyOrdersPage key={openOrderCode ?? 'list'} onNavigate={navigate} openCode={openOrderCode} />
        : <LoginPage setView={navigate} onAuthSuccess={handleAuthSuccess} notice="Hãy đăng nhập để xem đơn hàng của bạn." />;
      break;
    case 'login':
      page = (
        <LoginPage
          setView={navigate}
          onAuthSuccess={handleAuthSuccess}
          notice={loginNotice}
        />
      );
      break;
    case 'register':
      page = <RegisterPage setView={navigate} onAuthSuccess={handleAuthSuccess} />;
      break;
    case '404':
      page = <NotFoundPage onNavigate={navigate} />;
      break;
    default:
      page = (
        <HomePage
          user={user}
          setView={navigate}
          onAdd={addToCart}
          onOpen={setDetail}
          onSearch={(kw) => { setSearchKeyword(kw); navigate('products'); }}
        />
      );
  }

  return (
    <Layout style={{ minHeight: '100vh', background: '#fff' }}>
      <AppHeader
        view={view}
        setView={(next) => { setOpenOrderCode(null); setSearchKeyword(''); navigate(next); }}
        cartCount={cartCount}
        user={user}
        onLogout={handleLogout}
      />
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
          extra={[
            <Button
              key="track"
              type="primary"
              size="large"
              onClick={() => {
                setOpenOrderCode(order.orderCode);
                setOrder(null);
                navigate('orders');
              }}
            >
              Theo dõi đơn hàng
            </Button>,
            <Button
              key="home"
              size="large"
              onClick={() => {
                setOrder(null);
                navigate('home');
              }}
            >
              Về trang chủ
            </Button>,
          ]}
        />
      </Modal>
    </Layout>
  );
};

export default function App() {
  return (
    <ConfigProvider
      locale={viVN}
      theme={{
        token: {
          colorPrimary: PRIMARY,
          colorInfo: PRIMARY,
          borderRadius: 6,
          fontFamily: "'Be Vietnam Pro', system-ui, sans-serif",
          colorTextBase: '#1F1F1F',
        },
      }}
    >
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=Be+Vietnam+Pro:wght@400;500;600;700;800&display=swap');
        body { margin: 0; background: #fff; }
        /* Mũi tên carousel banner: nút tròn trắng dễ thấy trên mọi ảnh */
        .ant-carousel .slick-prev, .ant-carousel .slick-next { width: 36px; height: 36px; background: rgba(255,255,255,.85); border-radius: 50%; z-index: 2; }
        .ant-carousel .slick-prev { inset-inline-start: 16px; }
        .ant-carousel .slick-next { inset-inline-end: 16px; }
        .ant-carousel .slick-prev::after, .ant-carousel .slick-next::after { border-color: #E3121B; inset-inline-start: 13px; top: 13px; }
      `}</style>
      <AntApp>
        <FoodieApp />
      </AntApp>
    </ConfigProvider>
  );
}