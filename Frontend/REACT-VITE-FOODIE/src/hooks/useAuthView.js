import { useState, useEffect, useCallback } from 'react';
import { USER_KEY, TOKEN_KEY, SESSION_EXPIRED_EVENT, storage } from '../utils/helpers';
import { authService } from '../services/auth.service';

/**
 * Quản lý người dùng đăng nhập + trang đang xem (điều hướng bằng state).
 * Trang quản trị nằm ở backend (ASP.NET Core MVC, /Admin), không còn trong React.
 */
export const useAuthView = (cartLength, message) => {
  const [user, setUser] = useState(() => storage.get(USER_KEY, null));
  const [view, setView] = useState('home');
  const [loginNotice, setLoginNotice] = useState('');

  const navigate = useCallback((next) => {
    if (next !== 'login') setLoginNotice('');
    setView(next);
    window.scrollTo({ top: 0 });
  }, []);

  // Phiên hết hạn / tài khoản bị khóa: axios.client phát sự kiện khi API trả 401
  useEffect(() => {
    const onExpired = () => {
      setUser(null);
      setLoginNotice('Phiên đăng nhập đã hết hạn hoặc tài khoản đã bị khóa. Vui lòng đăng nhập lại.');
      setView('login');
    };
    window.addEventListener(SESSION_EXPIRED_EVENT, onExpired);
    return () => window.removeEventListener(SESSION_EXPIRED_EVENT, onExpired);
  }, []);

  // Mở trang mà còn token cũ => hỏi server token còn hợp lệ không, đồng thời cập nhật tên / vai trò mới nhất.
  // Token không hợp lệ => axios.client tự xóa và phát sự kiện ở trên.
  useEffect(() => {
    if (!storage.get(TOKEN_KEY)) return;
    authService
      .me()
      .then((fresh) => {
        if (!fresh) return;
        storage.set(USER_KEY, fresh);
        setUser(fresh);
      })
      .catch(() => { /* lỗi mạng: giữ nguyên trạng thái, request sau sẽ thử lại */ });
  }, []);

  const handleAuthSuccess = ({ token, user: u }, mode) => {
    storage.set(TOKEN_KEY, token);
    storage.set(USER_KEY, u);
    setUser(u);
    message?.success(mode === 'register' ? 'Tạo tài khoản thành công!' : `Xin chào ${u.name}!`);

    if (mode === 'login' && cartLength > 0) navigate('cart');
    else navigate('home');
  };

  // Cập nhật thông tin hiển thị (tên, ảnh đại diện) sau khi sửa hồ sơ
  const updateUser = useCallback((changes) => {
    setUser((prev) => {
      if (!prev) return prev;
      const next = { ...prev, ...changes };
      storage.set(USER_KEY, next);
      return next;
    });
  }, []);

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
    updateUser,
  };
};
