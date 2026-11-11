import React, { useEffect } from 'react';
import { MapContainer, TileLayer, Marker, Popup, useMap } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import contact from '../../config/contact';
import { PRIMARY } from '../../utils/helpers';

// Biểu tượng ngôi nhà (SVG) trong hình tròn đỏ, mũi nhọn chỉ đúng vị trí shop
const houseIcon = L.divIcon({
    className: 'foodie-house-marker',
    html: `
        <div style="width:44px;height:44px;border-radius:50% 50% 50% 0;background:${PRIMARY};
                    transform:rotate(-45deg);display:grid;place-items:center;
                    box-shadow:0 4px 10px rgba(0,0,0,.35);border:3px solid #fff;">
            <svg viewBox="0 0 24 24" width="22" height="22" style="transform:rotate(45deg)" aria-hidden="true">
                <path fill="#fff" d="M12 3 1.5 12H5v9h5.5v-6h3v6H19v-9h3.5z"/>
            </svg>
        </div>`,
    iconSize: [44, 44],
    iconAnchor: [4, 44],       // góc nhọn phía dưới bên trái (đã xoay) trỏ đúng tọa độ
    popupAnchor: [18, -44],
});

// Leaflet tính sai kích thước khi bản đồ nằm trong Modal đang mở (hiện vùng xám) => đo lại sau khi hiện
const FixSizeOnShow = () => {
    const map = useMap();
    useEffect(() => {
        const t = setTimeout(() => map.invalidateSize(), 250);
        return () => clearTimeout(t);
    }, [map]);
    return null;
};

/**
 * Bản đồ vị trí shop (OpenStreetMap, không cần API key).
 *  - interactive=false: bản đồ thu nhỏ trong footer (không kéo/zoom, chỉ để bấm mở bản đồ lớn)
 */
const ShopMap = ({ height = 220, zoom = 16, interactive = true, showPopup = false }) => (
    // z-index: 0 tạo lớp xếp chồng riêng => các lớp của Leaflet (z-index 400+) không đè lên header dính
    <div style={{ position: 'relative', zIndex: 0, height, borderRadius: 8, overflow: 'hidden' }}>
        <MapContainer
            center={[contact.lat, contact.lng]}
            zoom={zoom}
            style={{ height: '100%', width: '100%' }}
            scrollWheelZoom={interactive}
            dragging={interactive}
            doubleClickZoom={interactive}
            touchZoom={interactive}
            boxZoom={interactive}
            keyboard={interactive}
            zoomControl={interactive}
            attributionControl={interactive}
        >
            <TileLayer
                attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
                url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
            />
            <Marker position={[contact.lat, contact.lng]} icon={houseIcon} interactive={interactive}>
                <Popup autoPan>
                    <strong>{contact.shopName}</strong>
                    <br />
                    {contact.address}
                </Popup>
            </Marker>
            {interactive && <FixSizeOnShow />}
            {showPopup && <OpenPopup />}
        </MapContainer>
    </div>
);

// Mở sẵn popup địa chỉ khi bản đồ lớn vừa hiện
const OpenPopup = () => {
    const map = useMap();
    useEffect(() => {
        const t = setTimeout(() => {
            map.eachLayer((layer) => { if (layer instanceof L.Marker) layer.openPopup(); });
        }, 400);
        return () => clearTimeout(t);
    }, [map]);
    return null;
};

export default ShopMap;
