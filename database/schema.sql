-- IWAS canonical schema v1. Run with a setup identity, never the runtime reader.
CREATE TABLE IF NOT EXISTS warehouse_metadata (
    id TINYINT NOT NULL PRIMARY KEY CHECK (id = 1),
    schema_version INT NOT NULL,
    source_version VARCHAR(100) NOT NULL,
    reliable_history_start DATE NULL,
    has_trusted_opening_stock BOOLEAN NOT NULL DEFAULT FALSE,
    source_name VARCHAR(255) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS items (
    item_id VARCHAR(64) NOT NULL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    unit VARCHAR(50) NOT NULL,
    category VARCHAR(100) NULL,
    holding_cost DECIMAL(18,4) NOT NULL CHECK (holding_cost >= 0),
    ordering_cost DECIMAL(18,4) NOT NULL CHECK (ordering_cost >= 0),
    description VARCHAR(4000) NOT NULL,
    catalogue_unit_price DECIMAL(18,4) NULL CHECK (catalogue_unit_price >= 0),
    price_source VARCHAR(255) NULL,
    price_availability_reason VARCHAR(255) NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS stock_movements (
    movement_id VARCHAR(64) NOT NULL PRIMARY KEY,
    item_id VARCHAR(64) NOT NULL,
    movement_date DATE NOT NULL,
    movement_sequence INT NOT NULL CHECK (movement_sequence >= 0),
    movement_type VARCHAR(7) NOT NULL CHECK (movement_type IN ('Receipt','Issue')),
    quantity DECIMAL(18,4) NOT NULL CHECK (quantity > 0),
    unit_purchase_price DECIMAL(18,4) NULL CHECK (unit_purchase_price >= 0),
    CONSTRAINT fk_movement_item FOREIGN KEY (item_id) REFERENCES items(item_id),
    UNIQUE KEY ux_movement_order (item_id, movement_date, movement_sequence)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- One item/order in v1; split deliveries require an explicit schema/mapping extension.
CREATE TABLE IF NOT EXISTS supplier_deliveries (
    delivery_record_key VARCHAR(64) NOT NULL PRIMARY KEY,
    purchase_order_id VARCHAR(64) NOT NULL UNIQUE,
    supplier_id VARCHAR(64) NOT NULL,
    supplier_name VARCHAR(255) NOT NULL,
    item_id VARCHAR(64) NOT NULL,
    promised_delivery_date DATE NOT NULL,
    actual_delivery_date DATE NULL,
    CONSTRAINT fk_delivery_item FOREIGN KEY (item_id) REFERENCES items(item_id),
    KEY ix_delivery_actual (actual_delivery_date, supplier_id),
    KEY ix_delivery_promised (promised_delivery_date, supplier_id),
    KEY ix_delivery_item (item_id, actual_delivery_date)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS requisitions (
    requisition_id VARCHAR(64) NOT NULL PRIMARY KEY,
    department VARCHAR(100) NOT NULL,
    requisition_date DATE NOT NULL,
    description VARCHAR(4000) NOT NULL,
    KEY ix_requisition_date (requisition_date, requisition_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
