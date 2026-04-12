-- SQLite


SELECT 
    COUNT(*) AS TotalRows, 
    COUNT(DISTINCT Url) AS UniqueUrls 
FROM News;

SELECT DATE(PublishedAt) AS PublishDate, COUNT(*) AS ArticleCount
FROM News
GROUP BY DATE(PublishedAt)
ORDER BY PublishDate DESC;

DELETE FROM News
WHERE rowid NOT IN (
    SELECT MIN(rowid)
    FROM News
    GROUP BY Url
);

SELECT DATE(PublishedAt) AS PublishDate, COUNT(*) AS ArticleCount
FROM News
GROUP BY DATE(PublishedAt)
ORDER BY PublishDate DESC;



SELECT Title, PublishedAt 
FROM News 
ORDER BY PublishedAt DESC 
LIMIT 5;

SELECT Title
FROM News
WHERE Url LIKE '%AI%';

SELECT Title, Url FROM News;

