import React from 'react';
import { Outlet, Link } from 'react-router-dom';

const App = () => {
  return (
    <div className="foodie-app-container" style={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      {/* 1. Thanh Navbar toàn cục */}
      <header style={{ backgroundColor: '#ff4757', color: '#fff', padding: '15px 30px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', boxShadow: '0 2px 5px rgba(0,0,0,0.1)' }}>
        <h2 style={{ margin: 0, fontSize: '24px', fontWeight: 'bold' }}>
          🍔 Foodie Store
        </h2>
        <nav style={{ display: 'flex', gap: '20px', fontSize: '16px' }}>
          <Link to="/" style={{ color: '#fff', textDecoration: 'none', fontWeight: '500' }}>Trang chủ</Link>
          <Link to="/products" style={{ color: '#fff', textDecoration: 'none', fontWeight: '500' }}>Sản phẩm</Link>
          <Link to="/login" style={{ color: '#fff', textDecoration: 'none', fontWeight: '500' }}>Đăng nhập</Link>
          <Link to="/register" style={{ color: '#fff', textDecoration: 'none', fontWeight: '500' }}>Đăng ký</Link>
        </nav>
      </header>

      {/* 2. Khu vực chứa nội dung các trang con (HomePage, ProductsPage,...) */}
      <main style={{ flex: 1, padding: '20px', backgroundColor: '#f5f6fa' }}>
        <Outlet />
      </main>

      {/* 3. Footer chân trang */}
      <footer style={{ backgroundColor: '#2f3640', color: '#f5f6fa', textAlign: 'center', padding: '15px', fontSize: '14px' }}>
        <p style={{ margin: 0 }}>© 2026 Foodie Web Project - Designed with ASP.NET Core & ReactJS.</p>
      </footer>
    </div>
  );
};

export default App;