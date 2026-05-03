import { existsSync, readFileSync, readdirSync, statSync } from 'fs';
import { dirname, join, relative } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const wwwroot = join(__dirname, 'output', 'wwwroot');
const siteOrigin = 'https://mohdali.dev';
const publicationDate = new Date().toISOString().slice(0, 10);
const forbiddenRoutePattern = /(mermaid-smoke|smoke-test|draft)/i;
const socialImageKeys = ['og:image', 'og:image:secure_url', 'twitter:image'];
const requiredMeta = {
  property: [
    'og:site_name',
    'og:title',
    'og:description',
    'og:type',
    'og:url',
    'og:image',
    'og:image:secure_url',
    'og:image:type',
    'og:image:alt'
  ],
  name: [
    'description',
    'twitter:card',
    'twitter:site',
    'twitter:creator',
    'twitter:title',
    'twitter:description',
    'twitter:image',
    'twitter:image:alt'
  ]
};

const failures = [];

function fail(message) {
  failures.push(message);
}

function readRequiredFile(path, label) {
  if (!existsSync(path)) {
    fail(`Missing ${label}: ${relative(wwwroot, path)}`);
    return '';
  }

  return readFileSync(path, 'utf8');
}

function decodeXml(value) {
  return value
    .replace(/&amp;/g, '&')
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&quot;/g, '"')
    .replace(/&apos;/g, "'");
}

function extractTagValues(xml, tagName) {
  const pattern = new RegExp(`<${tagName}\\b[^>]*>([\\s\\S]*?)<\\/${tagName}>`, 'gi');
  return [...xml.matchAll(pattern)].map(match => decodeXml(match[1].trim()));
}

function extractBlocks(xml, tagName) {
  const pattern = new RegExp(`<${tagName}\\b[^>]*>([\\s\\S]*?)<\\/${tagName}>`, 'gi');
  return [...xml.matchAll(pattern)].map(match => match[1]);
}

function extractFirstTagValue(xml, tagName) {
  return extractTagValues(xml, tagName)[0] ?? '';
}

function routeForUrl(url) {
  if (!URL.canParse(url)) {
    fail(`Invalid sitemap URL: ${url}`);
    return null;
  }

  const parsed = new URL(url);

  if (parsed.origin !== siteOrigin) {
    fail(`Sitemap URL is not on ${siteOrigin}: ${url}`);
    return null;
  }

  return decodeURIComponent(parsed.pathname || '/');
}

function htmlPathForRoute(route) {
  const normalizedRoute = route === '/' ? '/' : route.replace(/\/+$/, '');

  if (normalizedRoute === '/') {
    return join(wwwroot, 'index.html');
  }

  if (normalizedRoute.endsWith('.html')) {
    return join(wwwroot, normalizedRoute);
  }

  return join(wwwroot, normalizedRoute, 'index.html');
}

function parseAttributes(source) {
  const attributes = new Map();
  const attributePattern = /([^\s=]+)\s*=\s*(?:"([^"]*)"|'([^']*)'|([^\s"'>]+))/g;

  for (const match of source.matchAll(attributePattern)) {
    attributes.set(match[1].toLowerCase(), match[2] ?? match[3] ?? match[4] ?? '');
  }

  return attributes;
}

function parseHead(html) {
  const head = html.match(/<head\b[^>]*>([\s\S]*?)<\/head>/i)?.[1] ?? '';
  const title = head.match(/<title\b[^>]*>([\s\S]*?)<\/title>/i)?.[1]?.trim() ?? '';
  const metaByProperty = new Map();
  const metaByName = new Map();
  const links = [];

  for (const match of head.matchAll(/<meta\b[^>]*>/gi)) {
    const attributes = parseAttributes(match[0]);
    const content = attributes.get('content')?.trim() ?? '';
    const property = attributes.get('property')?.toLowerCase();
    const name = attributes.get('name')?.toLowerCase();

    if (property) {
      metaByProperty.set(property, content);
    }

    if (name) {
      metaByName.set(name, content);
    }
  }

  for (const match of head.matchAll(/<link\b[^>]*>/gi)) {
    links.push(parseAttributes(match[0]));
  }

  return { title, metaByProperty, metaByName, links };
}

function requirePresent(page, label, value) {
  if (!value?.trim()) {
    fail(`${page}: missing ${label}`);
  }
}

function assertAbsoluteHttps(page, label, value) {
  requirePresent(page, label, value);

  if (!value) {
    return null;
  }

  if (!URL.canParse(value)) {
    fail(`${page}: ${label} is not an absolute URL: ${value}`);
    return null;
  }

  const parsed = new URL(value);

  if (parsed.protocol !== 'https:') {
    fail(`${page}: ${label} must use https: ${value}`);
  }

  return parsed;
}

function assertSocialImage(page, key, value) {
  const parsed = assertAbsoluteHttps(page, key, value);
  if (!parsed) {
    return;
  }

  const localPath = localPathForImageUrl(parsed);

  if (localPath && !existsSync(localPath)) {
    fail(`${page}: ${key} points to missing local image ${parsed.pathname}`);
  }
}

function localPathForImageUrl(value) {
  const parsed = value instanceof URL
    ? value
    : URL.canParse(value) ? new URL(value) : null;

  if (!parsed || parsed.origin !== siteOrigin) {
    return null;
  }

  return join(wwwroot, decodeURIComponent(parsed.pathname));
}

function inferImageType(value) {
  const pathname = URL.canParse(value)
    ? new URL(value).pathname.toLowerCase()
    : value.split(/[?#]/)[0].toLowerCase();

  if (pathname.endsWith('.jpg') || pathname.endsWith('.jpeg')) {
    return 'image/jpeg';
  }

  if (pathname.endsWith('.webp')) {
    return 'image/webp';
  }

  if (pathname.endsWith('.svg')) {
    return 'image/svg+xml';
  }

  if (pathname.endsWith('.png')) {
    return 'image/png';
  }

  return '';
}

function readImageDimensions(path) {
  if (!existsSync(path)) {
    return null;
  }

  const bytes = readFileSync(path);

  if (bytes.length >= 24 &&
      bytes[0] === 0x89 &&
      bytes[1] === 0x50 &&
      bytes[2] === 0x4E &&
      bytes[3] === 0x47) {
    return {
      width: bytes.readUInt32BE(16),
      height: bytes.readUInt32BE(20)
    };
  }

  if (bytes.length >= 4 && bytes[0] === 0xFF && bytes[1] === 0xD8) {
    return readJpegDimensions(bytes);
  }

  return null;
}

function readJpegDimensions(bytes) {
  let index = 2;

  while (index < bytes.length - 9) {
    if (bytes[index] !== 0xFF) {
      index++;
      continue;
    }

    while (index < bytes.length && bytes[index] === 0xFF) {
      index++;
    }

    if (index >= bytes.length) {
      return null;
    }

    const marker = bytes[index++];

    if (marker === 0xD8 || marker === 0xD9) {
      continue;
    }

    if (index + 1 >= bytes.length) {
      return null;
    }

    const segmentLength = bytes.readUInt16BE(index);

    if (segmentLength < 2 || index + segmentLength > bytes.length) {
      return null;
    }

    if (isJpegStartOfFrame(marker)) {
      return {
        width: bytes.readUInt16BE(index + 5),
        height: bytes.readUInt16BE(index + 3)
      };
    }

    index += segmentLength;
  }

  return null;
}

function isJpegStartOfFrame(marker) {
  return marker === 0xC0 ||
    marker === 0xC1 ||
    marker === 0xC2 ||
    marker === 0xC3 ||
    marker === 0xC5 ||
    marker === 0xC6 ||
    marker === 0xC7 ||
    marker === 0xC9 ||
    marker === 0xCA ||
    marker === 0xCB ||
    marker === 0xCD ||
    marker === 0xCE ||
    marker === 0xCF;
}

function assertDeclaredImageDimensions(page, imageUrl, width, height) {
  const localPath = localPathForImageUrl(imageUrl);

  if (!localPath || !existsSync(localPath)) {
    return;
  }

  const dimensions = readImageDimensions(localPath);

  if (!dimensions) {
    return;
  }

  if (Number(width) !== dimensions.width || Number(height) !== dimensions.height) {
    fail(`${page}: og:image dimensions ${width}x${height} do not match actual image ${dimensions.width}x${dimensions.height}`);
  }
}

function findMermaidTags(html) {
  return [...html.matchAll(/<div\b[^>]*>/gi)]
    .map(match => match[0])
    .filter(tag => (parseAttributes(tag).get('class') ?? '').split(/\s+/).includes('mermaid'));
}

function parseSitemapEntry(block) {
  return {
    loc: extractFirstTagValue(block, 'loc'),
    lastmod: extractFirstTagValue(block, 'lastmod')
  };
}

function assertPositiveInteger(page, label, value) {
  requirePresent(page, label, value);

  if (value && !/^[1-9]\d*$/.test(value)) {
    fail(`${page}: ${label} must be a positive integer`);
  }
}

function assertNotFutureDate(label, value) {
  if (!value) {
    return;
  }

  const normalized = value.match(/^\d{4}-\d{2}-\d{2}/)?.[0];

  if (normalized && normalized > publicationDate) {
    fail(`${label} is future-dated (${normalized}) relative to ${publicationDate}`);
  }
}

function assertNotFuturePubDate(label, value) {
  requirePresent(label, 'pubDate', value);

  if (!value) {
    return;
  }

  const parsed = new Date(value);

  if (Number.isNaN(parsed.getTime())) {
    fail(`${label}: pubDate is not parseable: ${value}`);
    return;
  }

  const normalized = parsed.toISOString().slice(0, 10);

  if (normalized > publicationDate) {
    fail(`${label}: pubDate is future-dated (${normalized}) relative to ${publicationDate}`);
  }
}

function validatePage(route, htmlPath) {
  const html = readRequiredFile(htmlPath, `HTML for ${route}`);

  if (!html) {
    return;
  }

  const page = route === '/' ? '/' : route.replace(/\/+$/, '');
  const head = parseHead(html);
  const canonical = head.links.find(link => (link.get('rel') ?? '').split(/\s+/).includes('canonical'))?.get('href') ?? '';

  if (/%%CODE_BLOCK_\d+%%/.test(html)) {
    fail(`${page}: contains unresolved markdown code block placeholders`);
  }

  const mermaidTags = findMermaidTags(html);
  const unprocessedMermaidTags = mermaidTags.filter(tag => !/\bdata-processed=(["'])true\1/i.test(tag));

  if (unprocessedMermaidTags.length > 0) {
    fail(`${page}: contains unprocessed Mermaid diagrams`);
  }

  if (mermaidTags.length > 0 && !/<svg\b/i.test(html)) {
    fail(`${page}: contains Mermaid diagrams without rendered SVG output`);
  }

  requirePresent(page, 'title', head.title);
  requirePresent(page, 'meta description', head.metaByName.get('description'));
  requirePresent(page, 'canonical link', canonical);
  assertAbsoluteHttps(page, 'canonical link', canonical);

  for (const key of requiredMeta.property) {
    requirePresent(page, key, head.metaByProperty.get(key));
  }

  for (const key of requiredMeta.name) {
    requirePresent(page, key, head.metaByName.get(key));
  }

  if (head.metaByName.get('twitter:card') !== 'summary_large_image') {
    fail(`${page}: twitter:card must be summary_large_image`);
  }

  assertAbsoluteHttps(page, 'og:url', head.metaByProperty.get('og:url'));
  const imageUrl = head.metaByProperty.get('og:image') ?? '';
  const imageWidth = head.metaByProperty.get('og:image:width') ?? '';
  const imageHeight = head.metaByProperty.get('og:image:height') ?? '';
  const inferredImageType = inferImageType(imageUrl);
  const declaredImageType = head.metaByProperty.get('og:image:type') ?? '';

  if (inferredImageType && declaredImageType !== inferredImageType) {
    fail(`${page}: og:image:type ${declaredImageType} does not match image URL type ${inferredImageType}`);
  }

  if (imageWidth || imageHeight) {
    assertPositiveInteger(page, 'og:image:width', imageWidth);
    assertPositiveInteger(page, 'og:image:height', imageHeight);
    assertDeclaredImageDimensions(page, imageUrl, imageWidth, imageHeight);
  }

  if (page.startsWith('/posts/') && !head.metaByProperty.get('article:published_time')) {
    fail(`${page}: missing article:published_time`);
  }

  if (page.startsWith('/posts/')) {
    assertNotFutureDate(`${page}: article:published_time`, head.metaByProperty.get('article:published_time'));
  }

  for (const key of socialImageKeys) {
    const source = key.startsWith('og:') ? head.metaByProperty : head.metaByName;
    assertSocialImage(page, key, source.get(key));
  }
}

function validateRssItem(block, index, sitemapUrlSet) {
  const label = `rss.xml item ${index + 1}`;
  const title = extractFirstTagValue(block, 'title');
  const link = extractFirstTagValue(block, 'link');
  const guid = extractFirstTagValue(block, 'guid');
  const pubDate = extractFirstTagValue(block, 'pubDate');
  const description = extractFirstTagValue(block, 'description');

  requirePresent(label, 'title', title);
  requirePresent(label, 'link', link);
  requirePresent(label, 'guid', guid);
  requirePresent(label, 'description', description);
  assertNotFuturePubDate(label, pubDate);

  if (link && guid && link !== guid) {
    fail(`${label}: link and guid differ`);
  }

  if (link && forbiddenRoutePattern.test(link)) {
    fail(`${label}: includes a draft/smoke/test route: ${link}`);
  }

  if (link && !sitemapUrlSet.has(link)) {
    fail(`${label}: link is missing from sitemap.xml: ${link}`);
  }

  if (link) {
    routeForUrl(link);
  }
}

function walkDirectories(root, visit) {
  if (!existsSync(root)) {
    return;
  }

  for (const item of readdirSync(root)) {
    const fullPath = join(root, item);

    if (statSync(fullPath).isDirectory()) {
      visit(fullPath);
      walkDirectories(fullPath, visit);
    }
  }
}

if (!existsSync(wwwroot)) {
  fail(`Missing prerender output directory: ${wwwroot}`);
} else {
  const sitemap = readRequiredFile(join(wwwroot, 'sitemap.xml'), 'sitemap.xml');
  const rss = readRequiredFile(join(wwwroot, 'rss.xml'), 'rss.xml');
  const sitemapEntries = extractBlocks(sitemap, 'url').map(parseSitemapEntry);
  const sitemapUrls = sitemapEntries.map(entry => entry.loc).filter(Boolean);
  const sitemapUrlSet = new Set(sitemapUrls);
  const rssItems = extractBlocks(rss, 'item');
  const rssUrls = [
    ...extractTagValues(rss, 'link'),
    ...extractTagValues(rss, 'guid')
  ];

  for (const url of sitemapUrls) {
    if (forbiddenRoutePattern.test(url)) {
      fail(`sitemap.xml includes a draft/smoke/test route: ${url}`);
    }
  }

  for (const url of rssUrls) {
    if (forbiddenRoutePattern.test(url)) {
      fail(`rss.xml includes a draft/smoke/test route: ${url}`);
    }
  }

  if (sitemapUrls.length === 0) {
    fail('sitemap.xml does not contain any <loc> URLs');
  }

  if (rssItems.length === 0) {
    fail('rss.xml does not contain any <item> entries');
  }

  sitemapEntries.forEach((entry, index) => {
    requirePresent(`sitemap.xml url ${index + 1}`, 'loc', entry.loc);
    requirePresent(`sitemap.xml url ${index + 1}`, 'lastmod', entry.lastmod);
    assertNotFutureDate(`sitemap.xml url ${index + 1} lastmod`, entry.lastmod);
  });

  rssItems.forEach((item, index) => validateRssItem(item, index, sitemapUrlSet));

  for (const url of sitemapUrls) {
    const route = routeForUrl(url);

    if (!route) {
      continue;
    }

    if (forbiddenRoutePattern.test(route)) {
      fail(`Sitemap contains forbidden route: ${route}`);
    }

    const htmlPath = htmlPathForRoute(route);

    if (!existsSync(htmlPath)) {
      fail(`Sitemap URL has no matching HTML file: ${url} -> ${relative(wwwroot, htmlPath)}`);
      continue;
    }

    validatePage(route, htmlPath);
  }

  walkDirectories(wwwroot, fullPath => {
    const outputPath = relative(wwwroot, fullPath).split('\\').join('/');

    if (forbiddenRoutePattern.test(outputPath)) {
      fail(`Output includes forbidden smoke/test/draft directory: ${outputPath}`);
    }
  });
}

if (failures.length > 0) {
  console.error('Prerender validation failed:');
  for (const failure of failures) {
    console.error(`- ${failure}`);
  }
  process.exit(1);
}

console.log('Prerender validation passed.');
