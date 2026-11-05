import { useState } from "react";
import { Button, Flex, Typography, Upload, message } from "antd";
import { DeleteOutlined, LoadingOutlined, PlusOutlined } from "@ant-design/icons";

import { PRIMARY } from "../../utils/helpers";
import { adminService } from "../../services/admin.service";
import { IMAGE_RULES, validateImageFile } from "../../utils/imageValidator";

const { Text } = Typography;

const BOX_SIZE = 128;

const boxStyle = {
    width: BOX_SIZE,
    height: BOX_SIZE,
    borderRadius: 16,
    border: "2px dashed #F0C9B8",
    background: "#FFFDFA",
    display: "grid",
    placeItems: "center",
    overflow: "hidden",
    cursor: "pointer",
    color: PRIMARY,
};

/**
 * Dùng như một control của Form.Item (nhận value / onChange).
 * value là URL ảnh trả về từ server, null khi chưa có ảnh.
 *
 * Props:
 * - onUploadingChange: (boolean) => void, để form khóa nút Lưu khi đang tải ảnh
 */
export default function ImageUploadField({ value, onChange, onUploadingChange }) {
    const [messageApi, contextHolder] = message.useMessage();
    const [uploading, setUploading] = useState(false);

    const setBusy = (busy) => {
        setUploading(busy);
        onUploadingChange?.(busy);
    };

    // Chặn file không hợp lệ trước khi gửi lên server
    const handleBeforeUpload = async (file) => {
        const result = await validateImageFile(file);
        if (!result.ok) {
            messageApi.error(result.message);
            return Upload.LIST_IGNORE;
        }
        return true;
    };

    const handleRequest = async ({ file, onSuccess, onError }) => {
        setBusy(true);
        try {
            const { url } = await adminService.uploadImage(file);
            onChange?.(url);
            onSuccess?.({ url });
            messageApi.success("Tải ảnh lên thành công");
        } catch (e) {
            onError?.(e);
            messageApi.error(e.message);
        } finally {
            setBusy(false);
        }
    };

    const maxMB = IMAGE_RULES.maxSizeBytes / 1024 / 1024;
    const formats = IMAGE_RULES.extensions.map((e) => e.toUpperCase()).join(", ");

    return (
        <Flex gap={16} align="center" wrap>
            {contextHolder}
            <Upload
                accept={IMAGE_RULES.extensions.map((e) => `.${e}`).join(",")}
                showUploadList={false}
                maxCount={1}
                disabled={uploading}
                beforeUpload={handleBeforeUpload}
                customRequest={handleRequest}
            >
                <div style={boxStyle} aria-label="Chọn ảnh món ăn">
                    {uploading ? (
                        <LoadingOutlined style={{ fontSize: 24 }} />
                    ) : value ? (
                        <img src={value} alt="Ảnh món ăn" style={{ width: "100%", height: "100%", objectFit: "cover" }} />
                    ) : (
                        <div style={{ textAlign: "center" }}>
                            <PlusOutlined style={{ fontSize: 22 }} />
                            <div style={{ marginTop: 6, fontSize: 13 }}>Tải ảnh lên</div>
                        </div>
                    )}
                </div>
            </Upload>

            <Flex vertical gap={6} align="flex-start">
                <Text type="secondary" style={{ fontSize: 13 }}>
                    {formats} · tối đa {maxMB}MB
                </Text>
                {value && !uploading && (
                    <Button size="small" danger icon={<DeleteOutlined />} onClick={() => onChange?.(null)}>
                        Xóa ảnh
                    </Button>
                )}
            </Flex>
        </Flex>
    );
}
