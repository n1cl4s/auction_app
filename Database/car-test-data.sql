-- Run only against an isolated test database after setting @sample_seller_id
-- to the ID of an existing test user in the same SQL session.
-- Unspecified car details and price are examples.
-- Mileage is stored in km; each Swedish mil value in a description is multiplied by 10.
INSERT INTO auctions (
    name, car_model, model_year, mileage_km, transmission, fuel_type, description,
    starting_price, end_time, seller_id
)
VALUES
    ('Mercedes-Benz W128 - 220 SE Cabriolet', 'Mercedes-Benz W128 - 220 SE Cabriolet', 1960,
     2990, 'manual', 'petrol', 'Model year: 1960; mileage: 299 mil; transmission: manual; fuel: petrol.', 2099900,
     UTC_TIMESTAMP(6) + INTERVAL 30 DAY, @sample_seller_id),
    ('DeSoto Firesweep Cabriolet', 'DeSoto Firesweep Cabriolet', 1958,
     50000, 'automatic', 'petrol', 'Model year: 1958; mileage: 5000 mil (example); transmission: automatic; fuel: petrol.', 1295000,
     UTC_TIMESTAMP(6) + INTERVAL 30 DAY, @sample_seller_id),
    ('Buick Special Cabriolet', 'Buick Special Cabriolet', 1954,
     70000, 'automatic', 'petrol', 'Model year: 1954; mileage: 7000 mil (example); transmission: automatic; fuel: petrol.', 449000,
     UTC_TIMESTAMP(6) + INTERVAL 30 DAY, @sample_seller_id),
    ('Pontiac Streamliner coupe', 'Pontiac Streamliner coupe', 1948,
     80000, 'automatic', 'petrol', 'Model year: 1948; mileage: 8000 mil (example); transmission: automatic; fuel: petrol.', 329000,
     UTC_TIMESTAMP(6) + INTERVAL 30 DAY, @sample_seller_id),
    ('Cadillac Series 62', 'Cadillac Series 62', 1947,
     60000, 'automatic', 'petrol', 'Model year: 1947; mileage: 6000 mil (example); transmission: automatic; fuel: petrol.', 629000,
     UTC_TIMESTAMP(6) + INTERVAL 30 DAY, @sample_seller_id),
    ('Dodge', 'Dodge', 1955,
     9420, 'manual', 'petrol', 'Model year: 1955 (example); mileage: 942 mil; transmission: manual; fuel: petrol.', 78000,
     UTC_TIMESTAMP(6) + INTERVAL 30 DAY, @sample_seller_id),
    ('Plymouth Fury', 'Plymouth Fury', 1958,
     45000, 'automatic', 'petrol', 'Model year: 1958; mileage: 4500 mil (example); transmission: automatic (example); fuel: petrol (example).', 750000,
     UTC_TIMESTAMP(6) + INTERVAL 30 DAY, @sample_seller_id),
    ('Chevrolet Impala Sport', 'Chevrolet Impala Sport', 1967,
     55000, 'automatic', 'petrol', 'Model year: 1967; mileage: 5500 mil (example); transmission: automatic (example); fuel: petrol (example).', 450000,
     UTC_TIMESTAMP(6) + INTERVAL 30 DAY, @sample_seller_id),
    ('Ford Thunderbird', 'Ford Thunderbird', 1960,
     65000, 'automatic', 'petrol', 'Model year: 1960; mileage: 6500 mil (example); transmission: automatic (example); fuel: petrol (example).', 395000,
     UTC_TIMESTAMP(6) + INTERVAL 30 DAY, @sample_seller_id),
    ('Morris Minor', 'Morris Minor', 1958,
     40000, 'manual', 'petrol', 'Model year: 1958 (example); mileage: 4000 mil (example); transmission: manual (example); fuel: petrol (example); starting price: 100000 SEK (example).', 100000,
     UTC_TIMESTAMP(6) + INTERVAL 30 DAY, @sample_seller_id),
    ('Ferrari 812 Superfast', 'Ferrari 812 Superfast', 2020,
     26500, 'automatic', 'petrol', 'Model year: 2020; mileage: 2650 mil; transmission: automatic; fuel: petrol; 6.5 V12; sold new in Sweden; bucket seats.', 3695000,
     UTC_TIMESTAMP(6) + INTERVAL 30 DAY, @sample_seller_id);
