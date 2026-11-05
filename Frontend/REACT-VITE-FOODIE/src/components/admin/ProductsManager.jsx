import { useState } from "react";
import { Alert, Button, Flex, Input, Popconfirm, Space, Table, Tag, Typography, message } from "antd";
import { DeleteOutlined, EditOutlined, PlusOutlined } from "@ant-design/icons";

import { PRIMARY, formatVND } from "../../utils/helpers";
import { PRODUCT_CATEGORIES } from "../../constants/admin";
import { adminService } from "../../services/admin.service";
import usePagedQuery from "../../hooks/usePagedQuery";
import ProductImage from "../products/ProductImage";
import QuickFilter from "./QuickFilter";
import ProductFormModal from "./ProductFormModal";

const { Text } = Typography;

const categoryOptions = PRODUCT_CATEGORIES.map((c) => ({ value: c, label: c }));

export default function ProductsManager() {
    const [messageApi, contextHolder] = message.useMessage();
    const { data, loading, error, filters, pagination, updateFilters, changePage, reload } = usePagedQuery(
        adminService.getProducts
    );

    const [formState, setFormState] = useState({ open: false, product: null });
    const [deletingId, setDeletingId] = useState(null);

    const openCreate = () => setFormState({ open: true, product: null });
    const openEdit = (product) => setFormState({ open: true, product });
    const closeForm = () => setFormState((s) => ({ ...s, open: false }));

    const handleSaved = (saved, isEdit) => {
        closeForm();
        messageApi.success(isEdit ? `Đã cập nhật "${saved.name}"` : `Đã thêm "${saved.name}" vào thực đơn`);
        // Món mới nằm đầu danh sách nên quay về trang 1
        if (!isEdit) changePage(1);
        reload();
    };

    const handleDelete = async (product) => {
        setDeletingId(product.id);
        try {
            await adminService.deleteProduct(product.id);
            messageApi.success(`Đã xóa "${product.name}"`);
            reload();
        } catch (e) {
            messageApi.error(e.message);
        } finally {
            setDeletingId(null);
        }
    };

    const columns = [
        {
            title: "Món ăn",
            key: "name",
            render: (_, p) => (
                <Flex gap={12} align="center">
                    <div style={{ width: 56, flexShrink: 0 }}>
                        <ProductImage product={p} height={56} radius={12} fontSize={28} />
                    </div>
                    <div style={{ minWidth: 0 }}>
                        <Text strong style={{ display: "block" }}>{p.name}</Text>
                        <Text type="secondary" ellipsis style={{ display: "block", maxWidth: 280, fontSize: 13 }}>
                            {p.description || "Chưa có mô tả"}
                        </Text>
                    </div>
                </Flex>
            ),
        },
        {
            title: "Danh mục",
            dataIndex: "category",
            key: "category",
            render: (c) => <Tag color="orange">{c}</Tag>,
        },
        {
            title: "Giá",
            dataIndex: "price",
            key: "price",
            align: "right",
            render: (price) => <Text strong style={{ color: PRIMARY }}>{formatVND(price)}</Text>,
        },
        {
            title: "Thao tác",
            key: "actions",
            width: 190,
            render: (_, p) => (
                <Space>
                    <Button icon={<EditOutlined />} onClick={() => openEdit(p)} aria-label={`Sửa ${p.name}`}>
                        Sửa
                    </Button>
                    <Popconfirm
                        title={`Xóa "${p.name}"?`}
                        description="Món sẽ biến mất khỏi thực đơn và không thể hoàn tác."
                        okText="Xóa"
                        cancelText="Giữ lại"
                        okButtonProps={{ danger: true, loading: deletingId === p.id }}
                        onConfirm={() => handleDelete(p)}
                    >
                        <Button danger icon={<DeleteOutlined />} aria-label={`Xóa ${p.name}`}>
                            Xóa
                        </Button>
                    </Popconfirm>
                </Space>
            ),
        },
    ];

    return (
        <>
            {contextHolder}

            <Flex justify="space-between" align="center" wrap gap={12} style={{ marginBottom: 16 }}>
                <Input.Search
                    allowClear
                    placeholder="Tìm theo tên món..."
                    onSearch={(v) => updateFilters({ keyword: v.trim() || undefined })}
                    style={{ maxWidth: 320 }}
                />
                <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
                    Thêm món
                </Button>
            </Flex>

            <div style={{ marginBottom: 16 }}>
                <QuickFilter
                    label="Lọc theo danh mục"
                    options={categoryOptions}
                    value={filters.category}
                    onChange={(category) => updateFilters({ category })}
                />
            </div>

            {error && (
                <Alert type="error" showIcon message="Không tải được danh sách món" description={error} style={{ marginBottom: 16 }} />
            )}

            <Table
                rowKey="id"
                columns={columns}
                dataSource={data.items}
                loading={loading}
                scroll={{ x: 720 }}
                locale={{ emptyText: "Không có món nào khớp bộ lọc." }}
                pagination={{
                    current: pagination.page,
                    pageSize: pagination.pageSize,
                    total: data.total,
                    showSizeChanger: true,
                    pageSizeOptions: [5, 10, 20],
                    showTotal: (total, [from, to]) => `${from}-${to} / ${total} món`,
                    onChange: changePage,
                }}
            />

            <ProductFormModal open={formState.open} product={formState.product} onCancel={closeForm} onSaved={handleSaved} />
        </>
    );
}
