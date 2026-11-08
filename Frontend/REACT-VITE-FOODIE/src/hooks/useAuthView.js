import { useState, useEffect } from 'react';
import { USER_KEY, TOKEN_KEY, storage } from '../utils/helpers';

export const useAuthView = (cartLength, message) => {
  // 1. Lấy user từ storage ra trước
  const [user, setUser] = useState(() => storage.get(USER_KEY, null));

  // 2. Khởi tạo view: Đọc thẳng từ storage xem role có phải Admin không
  const [view, setView] = useState(() => {
    const savedUser = storage.get(USER_KEY, null);
    return savedUser?.role === 'Admin' ? 'admin' : 'home';
  });

  const [loginNotice, setLoginNotice] = useState('');

  // 3. XÓA BỎ useEffect ghi đè storage cũ! 
  // Chỉ cập nhật storage khi đăng nhập / đăng xuất thực sự trong handleAuthSuccess / handleLogout

  const navigate = (next) => {
    if (next !== 'login') setLoginNotice('');
    setView(next);
    window.scrollTo({ top: 0 });
  };

  const handleAuthSuccess = ({ token, user: u }, mode) => {
    // Lưu thẳng vào storage trước
    storage.set(TOKEN_KEY, token);
    storage.set(USER_KEY, u);

    setUser(u);
    message?.success(mode === 'register' ? 'Tạo tài khoản thành công!' : `Xin chào ${u.name}!`);

    // Chuyển view
    if (u?.role === 'Admin') {
      navigate('admin');
    } else if (mode === 'login' && cartLength > 0) {
      navigate('cart');
    } else {
      navigate('home');
    }
  };

  const handleLogout = () => {
    storage.remove(TOKEN_KEY);
    storage.remove(USER_KEY);
    setUser(null);
    message?.info('Bạn đã đăng xuất');
    navigate('home');
  };

  return {
    view,
    user,
    loginNotice,
    setLoginNotice,
    navigate,
    handleAuthSuccess,
    handleLogout,
  };
};