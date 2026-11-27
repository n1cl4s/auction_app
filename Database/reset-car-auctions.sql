-- DESTRUCTIVE: deletes all data in simple_car_auctions.
-- Run only for local development or isolated test databases, then run car-auctions.sql.
DROP DATABASE IF EXISTS simple_car_auctions;
CREATE DATABASE simple_car_auctions CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
