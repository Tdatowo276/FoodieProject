import axiosClient from "./axios.client";

/**
 * Đánh giá món ăn. Chỉ khách có đơn ĐÃ HOÀN THÀNH chứa món mới đánh giá được (server kiểm tra).
 */
const reviewService = {
    // GET /api/products/{id}/reviews  =>  { summary: { average, count, stars: [1★..5★] }, reviews: Paged<ReviewDto> }
    getForProduct(productId, { page = 1, pageSize = 5 } = {}) {
        return axiosClient.get(`/products/${productId}/reviews`, { params: { page, pageSize } });
    },

    // GET /api/products/{id}/reviews/me  =>  { canReview, reason, review }
    getMine(productId) {
        return axiosClient.get(`/products/${productId}/reviews/me`);
    },

    // PUT: thêm mới hoặc sửa đánh giá của tôi
    save(productId, { rating, comment }) {
        return axiosClient.put(`/products/${productId}/reviews/me`, { rating, comment: comment || null });
    },

    remove(productId) {
        return axiosClient.delete(`/products/${productId}/reviews/me`);
    },
};

export { reviewService };
