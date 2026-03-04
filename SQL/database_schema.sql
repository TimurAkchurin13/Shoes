-- Полная структура базы данных для ООО "Обувь"
-- База данных: Shoes
-- СУБД: PostgreSQL

-- Создание таблицы ролей пользователей
CREATE TABLE user_role (
    id SERIAL PRIMARY KEY,
    role VARCHAR(50) NOT NULL
);

-- Создание таблицы пользователей
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    role_id INTEGER NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    middle_name VARCHAR(100),
    login VARCHAR(150) NOT NULL UNIQUE,
    password VARCHAR(50) NOT NULL,
    FOREIGN KEY (role_id) REFERENCES user_role(id)
);

-- Создание таблицы единиц измерения
CREATE TABLE unit_of_measure (
    id SERIAL PRIMARY KEY,
    unit_name VARCHAR(20) NOT NULL
);

-- Создание таблицы категорий товаров
CREATE TABLE category (
    id SERIAL PRIMARY KEY,
    category_name VARCHAR(50) NOT NULL
);

-- Создание таблицы наименований товаров
CREATE TABLE product_name (
    id SERIAL PRIMARY KEY,
    product_name VARCHAR(100) NOT NULL
);

-- Создание таблицы поставщиков
CREATE TABLE supplier (
    id SERIAL PRIMARY KEY,
    supplier_name VARCHAR(100) NOT NULL
);

-- Создание таблицы производителей
CREATE TABLE manufacturer (
    id SERIAL PRIMARY KEY,
    manufacturer_name VARCHAR(100) NOT NULL
);

-- Создание таблицы пунктов выдачи
CREATE TABLE pickup_points (
    id SERIAL PRIMARY KEY,
    point_code VARCHAR(20) NOT NULL,
    city VARCHAR(100) NOT NULL,
    street VARCHAR(100) NOT NULL,
    house_number VARCHAR(20) NOT NULL
);

-- Создание таблицы товаров
CREATE TABLE products (
    article VARCHAR(20) PRIMARY KEY,
    product_name_id INTEGER NOT NULL,
    unit_id INTEGER NOT NULL,
    price DECIMAL(10,2) NOT NULL,
    supplier_id INTEGER NOT NULL,
    manufacturer_id INTEGER NOT NULL,
    category_id INTEGER NOT NULL,
    current_discount DECIMAL(5,2),
    stock_quantity INTEGER NOT NULL,
    description TEXT,
    photo VARCHAR(100),
    FOREIGN KEY (product_name_id) REFERENCES product_name(id),
    FOREIGN KEY (unit_id) REFERENCES unit_of_measure(id),
    FOREIGN KEY (supplier_id) REFERENCES supplier(id),
    FOREIGN KEY (manufacturer_id) REFERENCES manufacturer(id),
    FOREIGN KEY (category_id) REFERENCES category(id)
);

-- Создание таблицы заказов
CREATE TABLE orders (
    order_number SERIAL PRIMARY KEY,
    order_date DATE NOT NULL DEFAULT CURRENT_DATE,
    delivery_date DATE NOT NULL,
    pickup_point_id INTEGER NOT NULL,
    client_id INTEGER NOT NULL,
    receipt_code VARCHAR(20) NOT NULL,
    order_status VARCHAR(50) NOT NULL,
    total_amount DECIMAL(10,2) NOT NULL DEFAULT 0,
    FOREIGN KEY (pickup_point_id) REFERENCES pickup_points(id),
    FOREIGN KEY (client_id) REFERENCES users(id)
);

-- Создание таблицы деталей заказа (товары в заказе)
CREATE TABLE order_details (
    id SERIAL PRIMARY KEY,
    order_number INTEGER NOT NULL,
    article VARCHAR(20) NOT NULL,
    quantity INTEGER NOT NULL CHECK (quantity > 0),
    unit_price DECIMAL(10,2) NOT NULL,
    discount DECIMAL(5,2) DEFAULT 0,
    total_price DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (order_number) REFERENCES orders(order_number) ON DELETE CASCADE,
    FOREIGN KEY (article) REFERENCES products(article)
);

-- Индексы для улучшения производительности
CREATE INDEX idx_users_login ON users(login);
CREATE INDEX idx_products_category ON products(category_id);
CREATE INDEX idx_products_manufacturer ON products(manufacturer_id);
CREATE INDEX idx_products_supplier ON products(supplier_id);
CREATE INDEX idx_orders_client ON orders(client_id);
CREATE INDEX idx_orders_date ON orders(order_date);
CREATE INDEX idx_order_details_order ON order_details(order_number);
CREATE INDEX idx_order_details_article ON order_details(article);

-- Комментарии к таблицам
COMMENT ON TABLE user_role IS 'Роли пользователей системы';
COMMENT ON TABLE users IS 'Пользователи системы';
COMMENT ON TABLE unit_of_measure IS 'Единицы измерения товаров';
COMMENT ON TABLE category IS 'Категории товаров';
COMMENT ON TABLE product_name IS 'Наименования товаров';
COMMENT ON TABLE supplier IS 'Поставщики товаров';
COMMENT ON TABLE manufacturer IS 'Производители товаров';
COMMENT ON TABLE pickup_points IS 'Пункты выдачи заказов';
COMMENT ON TABLE products IS 'Товары';
COMMENT ON TABLE orders IS 'Заказы';
COMMENT ON TABLE order_details IS 'Детали заказов (состав заказа)';

