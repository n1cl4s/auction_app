-- Fresh database for car auctions; this script does not migrate existing tables.
CREATE DATABASE IF NOT EXISTS simple_car_auctions CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE simple_car_auctions;

CREATE TABLE IF NOT EXISTS users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    email VARCHAR(254) NOT NULL UNIQUE,
    password_hash VARCHAR(512) NOT NULL
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS auctions (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    car_model VARCHAR(200),
    model_year SMALLINT UNSIGNED,
    description TEXT,
    color VARCHAR(50),
    starting_price DECIMAL(12,2),
    end_time DATETIME(6) NOT NULL COMMENT 'UTC',
    buy_now_price DECIMAL(12,2),
    seller_id INT NOT NULL,
    FOREIGN KEY (seller_id) REFERENCES users(id),
    CHECK (starting_price >= 0),
    CHECK (buy_now_price >= 0)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS bids (
    id INT AUTO_INCREMENT PRIMARY KEY,
    amount DECIMAL(12,2) NOT NULL,
    time DATETIME(6) NOT NULL COMMENT 'UTC',
    buyer_id INT NOT NULL,
    auction_id INT NOT NULL,
    FOREIGN KEY (buyer_id) REFERENCES users(id),
    FOREIGN KEY (auction_id) REFERENCES auctions(id),
    INDEX auction_amount (auction_id, amount)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS auction_images (
    id INT AUTO_INCREMENT PRIMARY KEY,
    auction_id INT NOT NULL,
    image_data LONGBLOB NOT NULL,
    content_type VARCHAR(127) NOT NULL,
    display_order INT UNSIGNED NOT NULL DEFAULT 0,
    FOREIGN KEY (auction_id) REFERENCES auctions(id),
    INDEX auction_image_order (auction_id, display_order)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS buy_now_purchases (
    auction_id INT PRIMARY KEY,
    buyer_id INT NOT NULL,
    price DECIMAL(12,2) NOT NULL,
    purchased_at DATETIME(6) NOT NULL COMMENT 'UTC',
    FOREIGN KEY (auction_id) REFERENCES auctions(id),
    FOREIGN KEY (buyer_id) REFERENCES users(id),
    CHECK (price >= 0)
) ENGINE=InnoDB;
