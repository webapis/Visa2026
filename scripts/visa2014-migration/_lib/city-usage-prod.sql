COPY (
  SELECT
    COALESCE(r."NameTm", '') AS region,
    c."NameTm" AS name_tm,
    (SELECT count(*) FROM "AddressesOfResidence" a WHERE a."CityID" = c."ID" AND (a."GCRecord" IS NULL OR a."GCRecord"=0)) AS aor,
    (SELECT count(*) FROM "Lodgings" l WHERE l."CityID" = c."ID" AND (l."GCRecord" IS NULL OR l."GCRecord"=0)) AS lodging,
    (SELECT count(*) FROM "Hotels" h WHERE h."CityID" = c."ID" AND (h."GCRecord" IS NULL OR h."GCRecord"=0)) AS hotel,
    (SELECT count(*) FROM "Hospitals" hp WHERE hp."CityID" = c."ID" AND (hp."GCRecord" IS NULL OR hp."GCRecord"=0)) AS hospital,
    CASE WHEN c."RegionID" IS NULL THEN 0 ELSE 1 END AS region_linked
  FROM "Cities" c
  LEFT JOIN "Regions" r ON r."ID" = c."RegionID"
  WHERE c."GCRecord" IS NULL OR c."GCRecord"=0
  ORDER BY r."NameTm", c."NameTm"
) TO STDOUT WITH CSV HEADER;
