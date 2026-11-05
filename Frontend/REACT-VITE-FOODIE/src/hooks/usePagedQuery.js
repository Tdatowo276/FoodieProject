import { useState, useEffect, useCallback } from "react";

export default function usePagedQuery(fetchFn, initialPageSize = 5) {
    const [data, setData] = useState({ items: [], total: 0 });
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [pagination, setPagination] = useState({ page: 1, pageSize: initialPageSize });
    const [filters, setFilters] = useState({});

    const loadData = useCallback(async () => {
        setLoading(true);
        setError("");
        try {
            const result = await fetchFn({
                page: pagination.page,
                pageSize: pagination.pageSize,
                ...filters,
            });
            setData(result);
        } catch (e) {
            setError(e.message || "Lỗi tải dữ liệu");
        } finally {
            setLoading(false);
        }
    }, [fetchFn, pagination.page, pagination.pageSize, filters]);

    useEffect(() => {
        loadData();
    }, [loadData]);

    const updateFilters = (newFilters) => {
        setFilters((prev) => ({ ...prev, ...newFilters }));
        setPagination((prev) => ({ ...prev, page: 1 })); // Reset về trang 1 khi đổi bộ lọc
    };

    const changePage = (page, pageSize) => {
        setPagination({ page, pageSize });
    };

    return {
        data,
        loading,
        error,
        filters,
        pagination,
        updateFilters,
        changePage,
        reload: loadData,
    };
}