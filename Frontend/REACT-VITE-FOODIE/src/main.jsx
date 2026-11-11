import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App.jsx'

// App tự điều hướng giữa các trang bằng state (xem hooks/useAuthView.js).
// Trước đây có khai báo thêm React Router cho /login, /register nhưng các trang đó được render
// thiếu props nên bị lỗi khi truy cập trực tiếp; nay mọi đường dẫn đều mở App.
ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
)
