import React, { useEffect, useRef } from 'react';
import { PRIMARY } from '../../utils/helpers';

const HEADER_HEIGHT = 68;   // chiều cao AppHeader (sticky)
const TABS_HEIGHT = 56;

/**
 * Thanh tab danh mục dính dưới header. Bấm tab => cuộn tới khu tương ứng;
 * cuộn trang => tab tự đổi theo khu đang xem (scroll-spy).
 *  - tabs: [{ key, label }]   key cũng là id của khu (section) trên trang
 */
const MenuTabs = ({ tabs, active, onChange }) => {
    const barRef = useRef(null);

    // Scroll-spy: theo dõi khu nào đang nằm gần mép trên màn hình
    useEffect(() => {
        const sections = tabs.map((t) => document.getElementById(t.key)).filter(Boolean);
        if (sections.length === 0) return undefined;

        const observer = new IntersectionObserver(
            (entries) => {
                const visible = entries.filter((e) => e.isIntersecting);
                if (visible.length > 0) onChange(visible[0].target.id);
            },
            { rootMargin: `-${HEADER_HEIGHT + TABS_HEIGHT + 8}px 0px -65% 0px` }
        );
        sections.forEach((s) => observer.observe(s));
        return () => observer.disconnect();
    }, [tabs, onChange]);

    // Giữ tab đang chọn luôn nằm trong vùng nhìn thấy khi thanh tab cuộn ngang (điện thoại)
    useEffect(() => {
        barRef.current?.querySelector(`[data-key="${active}"]`)?.scrollIntoView({ block: 'nearest', inline: 'center' });
    }, [active]);

    const goTo = (key) => {
        const el = document.getElementById(key);
        if (!el) return;
        const top = el.getBoundingClientRect().top + window.scrollY - HEADER_HEIGHT - TABS_HEIGHT - 4;
        window.scrollTo({ top, behavior: 'smooth' });
        onChange(key);
    };

    return (
        <nav
            aria-label="Danh mục món"
            style={{
                position: 'sticky',
                top: HEADER_HEIGHT,
                zIndex: 40,
                background: '#fff',
                borderBottom: `3px solid ${PRIMARY}`,
                boxShadow: '0 4px 10px rgba(0,0,0,.04)',
            }}
        >
            <div
                ref={barRef}
                role="tablist"
                style={{ display: 'flex', overflowX: 'auto', scrollbarWidth: 'none', maxWidth: 1180, margin: '0 auto' }}
            >
                {tabs.map((t) => {
                    const selected = t.key === active;
                    return (
                        <button
                            key={t.key}
                            data-key={t.key}
                            type="button"
                            role="tab"
                            aria-selected={selected}
                            onClick={() => goTo(t.key)}
                            style={{
                                flex: '1 0 auto',
                                minWidth: 120,
                                height: TABS_HEIGHT,
                                padding: '0 20px',
                                border: 'none',
                                cursor: 'pointer',
                                fontSize: 16,
                                fontFamily: 'inherit',
                                fontWeight: selected ? 700 : 500,
                                background: selected ? PRIMARY : '#fff',
                                color: selected ? '#fff' : '#222',
                                whiteSpace: 'nowrap',
                                transition: 'background .15s, color .15s',
                            }}
                        >
                            {t.label}
                        </button>
                    );
                })}
            </div>
        </nav>
    );
};

export default MenuTabs;
