import { Alert, Flex, Input, Table, Tag, Typography } from "antd";

import { PRIMARY, formatVND } from "../../utils/helpers";
import { ORDER_STATUS_META, PAYMENT_LABEL } from "../../constants/admin";
import { adminService } from "../../services/admin.service";
import usePagedQuery from "../../hooks/usePagedQuery";
import QuickFilter from "./QuickFilter";

const { Text } = Typography;

const statusOptions = Object.entries(ORDER_STATUS_META).map(([value, { label }]) => ({ value, label }));

const formatDateTime = (iso) =>
    new Date(iso).toLocaleString("vi-VN", {
        hour: "2-digit",
        minute: "2-digit",
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
    });

const columns = [
    { title: "Mã đơn", dataIndex: "code", key: "code", render: (code) => <Text strong>{code}</Text> },
    { title: "Khách hàng", dataIndex: "customer", key: "customer" },
    { title: "Ngày đặt", dataIndex: "createdAt", key: "createdAt", render: formatDateTime },
    {
        title: "Tổng tiền",
        dataIndex: "total",
        key: "total",
        align: "right",
        render: (total) => <Text strong style={{ color: PRIMARY }}>{formatVND(total)}</Text>,
    },
    { title: "Thanh toán", dataIndex: "payment", key: "payment", render: (p) => PAYMENT_LABEL[p] ?? p },
    {
        title: "Trạng thái",
        dataIndex: "status",
        key: "status",
        render: (status) => {
            const meta = ORDER_STATUS_META[status];
            return <Tag color={meta?.color}>{meta?.label ?? status}</Tag>;
        },
    },
];

export default function OrdersTable() {
    const { data, loading, error, filters, pagination, updateFilters, changePage } = usePagedQuery(
        adminService.getOrders
    );

    return (
        <>
            <Flex justify="space-between" align="center" wrap gap={12} style={{ marginBottom: 16 }}>
                <QuickFilter
                    label="Lọc theo trạng thái"
                    options={statusOptions}
                    value={filters.status}
                    onChange={(status) => updateFilters({ status })}
                />
                <Input.Search
                    allowClear
                    placeholder="Tìm mã đơn hoặc tên khách..."
                    onSearch={(v) => updateFilters({ keyword: v.trim() || undefined })}
                    style={{ maxWidth: 300 }}
                />
            </Flex>

            {error && (
                <Alert type="error" showIcon message="Không tải được danh sách đơn hàng" description={error} style={{ marginBottom: 16 }} />
            )}

            <Table
                rowKey="code"
                columns={columns}
                dataSource={data.items}
                loading={loading}
                scroll={{ x: 720 }}
                locale={{ emptyText: "Không có đơn hàng nào khớp bộ lọc." }}
                pagination={{
                    current: pagination.page,
                    pageSize: pagination.pageSize,
                    total: data.total,
                    showSizeChanger: true,
                    pageSizeOptions: [5, 10, 20],
                    showTotal: (total, [from, to]) => `${from}-${to} / ${total} đơn`,
                    onChange: changePage,
                }}
            />
        </>
    );
}
