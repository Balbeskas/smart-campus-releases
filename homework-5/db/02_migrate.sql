-- =====================================================================
-- CAMPUS-104 | Фаза 2. MIGRATE — перенос исторических данных price > amount
--
-- Выполняется после того, как ВСЕ экземпляры сервиса обновлены до v1.9.0
-- (они уже пишут в обе колонки). Перенос идет пакетами по 5 000 строк с COMMIT
-- после каждого пакета: блокировки строк короткие, кассы не ждут,
-- а прерванный перенос можно просто запустить снова — обработаются только
-- строки, где amount еще пустой.
-- =====================================================================
CREATE OR REPLACE PROCEDURE campus104_backfill_amount(batch_size int DEFAULT 5000)
LANGUAGE plpgsql
AS $$
DECLARE
    moved   int;
    total   int := 0;
BEGIN
    LOOP
        UPDATE canteen_checks c
           SET amount = c.price
         WHERE c.id IN (SELECT id FROM canteen_checks
                         WHERE amount IS NULL AND price IS NOT NULL
                         LIMIT batch_size
                         FOR UPDATE SKIP LOCKED);
        GET DIAGNOSTICS moved = ROW_COUNT;
        EXIT WHEN moved = 0;
        total := total + moved;
        COMMIT;                                   -- фиксируем пакет
        RAISE NOTICE 'CAMPUS-104: перенесено % строк (всего %)', moved, total;
    END LOOP;
    RAISE NOTICE 'CAMPUS-104: перенос завершен, всего строк: %', total;
END;
$$;

CALL campus104_backfill_amount();

-- Контроль: расхождений между колонками быть не должно
SELECT count(*)                                         AS total_checks,
       count(*) FILTER (WHERE amount IS NULL)           AS without_amount,
       count(*) FILTER (WHERE amount IS DISTINCT FROM price
                          AND price IS NOT NULL)        AS mismatched
  FROM canteen_checks;
