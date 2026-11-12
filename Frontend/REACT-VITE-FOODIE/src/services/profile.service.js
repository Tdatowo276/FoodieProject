import axiosClient from "./axios.client";

/**
 * Hồ sơ cá nhân của người dùng đang đăng nhập (GET/PUT /api/profile...).
 * Không truyền id: server luôn lấy tài khoản từ token, nên không sửa được hồ sơ người khác.
 */
const profileService = {
    // => { id, name, email, role, avatarUrl, defaultAddress: { recipientName, phone, address } | null, createdAt }
    get() {
        return axiosClient.get("/profile");
    },

    updateName(name) {
        return axiosClient.put("/profile", { name });
    },

    changePassword({ currentPassword, newPassword, confirmPassword }) {
        return axiosClient.put("/profile/password", { currentPassword, newPassword, confirmPassword });
    },

    uploadAvatar(file) {
        const form = new FormData();
        form.append("file", file);
        // axios tự thêm boundary cho multipart khi gửi FormData
        return axiosClient.post("/profile/avatar", form, { headers: { "Content-Type": "multipart/form-data" } });
    },

    removeAvatar() {
        return axiosClient.delete("/profile/avatar");
    },

    saveAddress({ recipientName, phone, address }) {
        return axiosClient.put("/profile/address", { recipientName, phone, address });
    },

    clearAddress() {
        return axiosClient.delete("/profile/address");
    },
};

export { profileService };
