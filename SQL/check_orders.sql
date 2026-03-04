-- Проверка заказов в базе данных

-- Количество заказов
SELECT COUNT(*) as total_orders FROM orders;

-- Все заказы с деталями
SELECT 
    o.order_number,
    o.order_date,
    o.delivery_date,
    o.client_id,
    o.total_amount,
    o.order_status,
    u.login as client_login,
    u.last_name || ' ' || u.first_name as client_name,
    COUNT(od.id) as detail_count
FROM orders o
LEFT JOIN users u ON o.client_id = u.id
LEFT JOIN order_details od ON o.order_number = od.order_number
GROUP BY o.order_number, o.order_date, o.delivery_date, o.client_id, o.total_amount, o.order_status, u.login, u.last_name, u.first_name
ORDER BY o.order_date DESC;

-- Проверка связанных таблиц
SELECT COUNT(*) as pickup_points_count FROM pickup_points;
SELECT COUNT(*) as users_count FROM users;

