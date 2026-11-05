import { useEffect, useState } from "react";
import { Form, Input, InputNumber, Modal, Select, message } from "antd";

import { PRODUCT_CATEGORIES } from "../../constants/admin";
import { adminService } from "../../services/admin.service";
import ImageUploadField from "./ImageUploadField";

const categoryOptions = PRODUCT_CATEGORIES.map((c) => ({ value: c, label: c }));

const formatPrice = (v) => `${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, ".") + " đ";
const parsePrice = (v) => (v ? v.replace(/\D/g, "") : "");

/**
 * Props:
 * - open:     hiện / ẩn modal
 * - product:  món đang sửa, null khi thêm mới
 * - onCancel: () => void
 * - onSaved:  (savedProduct, isEdit) => void
 */
export default function ProductFormModal({ open, product, onCancel, onSaved }) {
    const [form] = Form.useForm();
    const [messageApi, contextHolder] = message.useMessage();
    const [submitting, setSubmitting] = useState(false);
    const [uploading, setUploading] = useState(false);

    const isEdit = !!product;

    // Nạp dữ liệu mỗi lần mở modal
    useEffect(() => {
        if (!open) return;
        form.resetFields();
        if (product) {
            form.setFieldsValue({
                name: product.name,
                price: product.price,
                category: product.category,
                description: product.description,
                imageUrl: product.imageUrl ?? null,
            });
        }
    }, [open, product, form]);

    const handleFinish = async (values) => {
        const payload = {
            name: values.name.trim(),
            price: values.price,
            category: values.category,
            description: values.description?.trim() || "",
            imageUrl: values.imageUrl ?? null,
        };

        setSubmitting(true);
        try {
            const saved = isEdit
                ? await adminService.updateProduct(product.id, payload)
                : await adminService.createProduct(payload);
            onSaved(saved, isEdit);
        } catch (e) {
            messageApi.error(e.message);
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <Modal
            open={open}
            title={isEdit ? "Chỉnh sửa món ăn" : "Thêm món ăn mới"}
            okText={isEdit ? "Lưu thay đổi" : "Thêm món"}
            cancelText="Hủy"
            onCancel={onCancel}
            onOk={() => form.submit()}
            confirmLoading={submitting}
            okButtonProps={{ disabled: uploading }}
            maskClosable={!submitting}
            forceRender
            centered
            width={560}
        >
            {contextHolder}
            <Form form={form} layout="vertical" onFinish={handleFinish} requiredMark="optional" style={{ marginTop: 16 }}>
                <Form.Item name="imageUrl" label="Hình ảnh">
                    <ImageUploadField onUploadingChange={setUploading} />
                </Form.Item>

                <Form.Item
                    name="name"
                    label="Tên món"
                    rules={[
                        { required: true, whitespace: true, message: "Vui lòng nhập tên món." },
                        { min: 2, message: "Tên món cần ít nhất 2 ký tự." },
                        { max: 80, message: "Tên món tối đa 80 ký tự." },
                    ]}
                >
                    <Input placeholder="Ví dụ: Phở bò tái nạm" />
                </Form.Item>

                <Form.Item
                    name="price"
                    label="Giá bán"
                    rules={[
                        { required: true, message: "Vui lòng nhập giá." },
                        { type: "number", min: 1000, message: "Giá tối thiểu là 1.000đ." },
                    ]}
                >
                    <InputNumber
                        style={{ width: "100%" }}
                        min={0}
                        max={10000000}
                        step={1000}
                        formatter={formatPrice}
                        parser={parsePrice}
                        placeholder="55.000 đ"
                    />
                </Form.Item>

                <Form.Item name="category" label="Danh mục" rules={[{ required: true, message: "Vui lòng chọn danh mục." }]}>
                    <Select options={categoryOptions} placeholder="Chọn danh mục" />
                </Form.Item>

                <Form.Item name="description" label="Mô tả" rules={[{ max: 300, message: "Mô tả tối đa 300 ký tự." }]}>
                    <Input.TextArea rows={3} maxLength={300} showCount placeholder="Nguyên liệu, hương vị, cách thưởng thức..." />
                </Form.Item>
            </Form>
        </Modal>
    );
}
