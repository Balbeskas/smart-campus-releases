-- CAMPUS-104 | Исходное состояние (версия v1.8.x) — только для стенда.
-- Таблица чеков столовой со старой колонкой price и набором исторических чеков.

CREATE TABLE IF NOT EXISTS canteen_checks (
    id           uuid PRIMARY KEY,
    student_card varchar(32)   NOT NULL,
    cashier_id   varchar(32)   NOT NULL,
    items        text          NOT NULL,
    price        numeric(10,2) NOT NULL CHECK (price > 0),
    status       varchar(16)   NOT NULL DEFAULT 'Paid',
    created_at   timestamptz   NOT NULL DEFAULT now()
);

-- 25 000 исторических чеков за сентябрь–октябрь (для проверки пакетного переноса)
INSERT INTO canteen_checks (id, student_card, cashier_id, items, price, created_at)
SELECT md5('check' || g)::uuid,
       'STU-' || lpad((g % 1800)::text, 6, '0'),
       'CASH-0' || (g % 3 + 1),
       (ARRAY['Борщ', 'Плов', 'Сырники', 'Котлета с пюре', 'Компот'])[g % 5 + 1],
       round((60 + (g % 240))::numeric, 2),
       timestamptz '2026-09-01 08:00+03' + (g || ' minutes')::interval
FROM generate_series(1, 25000) AS g
ON CONFLICT DO NOTHING;
