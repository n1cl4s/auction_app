-- Run only against an isolated test database after setting these variables:
-- @sample_auction_id: the ID of an existing sample auction.
-- @sample_image_server_path: the absolute image path visible to the MySQL server.
-- @sample_image_content_type: the image MIME type, such as image/png or image/jpeg.
INSERT INTO auction_images (auction_id, image_data, content_type, display_order)
VALUES (@sample_auction_id, LOAD_FILE(@sample_image_server_path), @sample_image_content_type, 0);
