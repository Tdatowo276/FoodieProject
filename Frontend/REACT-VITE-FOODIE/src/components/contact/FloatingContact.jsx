import React, { useEffect, useState } from 'react';
import { Tooltip } from 'antd';
import { PhoneFilled, MailFilled, EnvironmentFilled } from '@ant-design/icons';
import { useContact } from './contactContext';

const SHOW_AFTER_PX = 300;   // cuộn xuống quá mức này thì hiện các nút

const buttons = [
    { key: 'phone', label: 'Gọi điện góp ý', icon: <PhoneFilled />, bg: 'linear-gradient(180deg, #FF2A2A, #D10A0A)' },
    { key: 'feedback', label: 'Gửi góp ý qua email', icon: <MailFilled />, bg: 'linear-gradient(180deg, #5CE7FF, #12C6E6)' },
    { key: 'map', label: 'Xem vị trí cửa hàng', icon: <EnvironmentFilled />, bg: 'linear-gradient(180deg, #FFB31A, #F08A00)' },
];

/** Ba nút liên hệ nổi ở góc phải, chỉ hiện khi người dùng đã cuộn xuống. */
const FloatingContact = () => {
    const { openPhone, openFeedback, openMap } = useContact();
    const [visible, setVisible] = useState(false);
    const actions = { phone: openPhone, feedback: openFeedback, map: openMap };

    useEffect(() => {
        const onScroll = () => setVisible(window.scrollY > SHOW_AFTER_PX);
        onScroll();
        window.addEventListener('scroll', onScroll, { passive: true });
        return () => window.removeEventListener('scroll', onScroll);
    }, []);

    return (
        <div
            aria-hidden={!visible}
            style={{
                position: 'fixed',
                right: 'max(16px, env(safe-area-inset-right))',
                bottom: 'calc(24px + env(safe-area-inset-bottom))',
                zIndex: 60,
                display: 'flex',
                flexDirection: 'column',
                gap: 14,
                transition: 'opacity .25s, transform .25s',
                opacity: visible ? 1 : 0,
                transform: visible ? 'translateY(0)' : 'translateY(20px)',
                pointerEvents: visible ? 'auto' : 'none',
            }}
        >
            {buttons.map((b) => (
                <Tooltip key={b.key} title={b.label} placement="left">
                    <button
                        type="button"
                        aria-label={b.label}
                        tabIndex={visible ? 0 : -1}
                        onClick={actions[b.key]}
                        style={{
                            width: 56,
                            height: 56,
                            borderRadius: '50%',
                            border: 'none',
                            cursor: 'pointer',
                            background: b.bg,
                            color: '#fff',
                            fontSize: 26,
                            display: 'grid',
                            placeItems: 'center',
                            boxShadow: '0 6px 16px rgba(0,0,0,.22)',
                        }}
                    >
                        {b.icon}
                    </button>
                </Tooltip>
            ))}
        </div>
    );
};

export default FloatingContact;
