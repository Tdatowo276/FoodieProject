import axiosClient from "./axios.client";

const feedbackService = {
    // POST /api/feedback  (không bắt buộc đăng nhập; có token thì server tự gắn tài khoản)
    // topic: food | delivery | service | website | other
    send({ name, email, phone, topic, rating, message }) {
        return axiosClient.post("/feedback", {
            name,
            email,
            phone: phone || null,
            topic,
            rating: rating || null,
            message,
        });
    },
};

export const FEEDBACK_TOPICS = [
    { value: "food", label: "Chất lượng món ăn" },
    { value: "delivery", label: "Giao hàng" },
    { value: "service", label: "Thái độ phục vụ" },
    { value: "website", label: "Trang web / ứng dụng" },
    { value: "other", label: "Khác" },
];

export { feedbackService };
