-- Reads app.params.input (path to .aseprite) and app.params.outprefix
-- (path+basename prefix for exported frames, e.g. ".../down" produces
-- down1.png, down2.png, ...). Flattens each frame, converts the detected
-- background color to true transparency (alpha=0, not just recolored),
-- and skips any frame that's pixel-identical to an already-exported one
-- for this file (duplicate-frame removal, same as was done by hand for
-- the "up" direction earlier).

local inputPath = app.params["input"]
local outPrefix = app.params["outprefix"]
local tolerance = 10 -- allow minor anti-aliasing variance around the sampled bg color

local spr = app.open(inputPath)
if not spr then
    error("Failed to open " .. inputPath)
end

local spec = spr.spec

local function flattenFrame(frameNumber)
    local img = Image(spec)
    img:drawSprite(spr, frameNumber)
    return img
end

local firstImg = flattenFrame(1)
local bgColor = firstImg:getPixel(0, 0)
local bgR = app.pixelColor.rgbaR(bgColor)
local bgG = app.pixelColor.rgbaG(bgColor)
local bgB = app.pixelColor.rgbaB(bgColor)
print(string.format("Detected background color for %s: R=%d G=%d B=%d", inputPath, bgR, bgG, bgB))

local function closeToBg(px)
    local r = app.pixelColor.rgbaR(px)
    local g = app.pixelColor.rgbaG(px)
    local b = app.pixelColor.rgbaB(px)
    return math.abs(r - bgR) <= tolerance
       and math.abs(g - bgG) <= tolerance
       and math.abs(b - bgB) <= tolerance
end

local function punchTransparency(img)
    for y = 0, img.height - 1 do
        for x = 0, img.width - 1 do
            local px = img:getPixel(x, y)
            if closeToBg(px) then
                img:putPixel(x, y, app.pixelColor.rgba(0, 0, 0, 0))
            end
        end
    end
end

local exportedImages = {}
local exportedCount = 0

for i = 1, #spr.frames do
    local img = flattenFrame(i)
    punchTransparency(img)

    local isDuplicate = false
    for _, prev in ipairs(exportedImages) do
        if img:isEqual(prev) then
            isDuplicate = true
            break
        end
    end

    if not isDuplicate then
        exportedCount = exportedCount + 1
        table.insert(exportedImages, img)
        local outPath = outPrefix .. exportedCount .. ".png"
        img:saveAs(outPath)
        print("Saved " .. outPath .. " (source frame " .. i .. ")")
    else
        print("Skipped source frame " .. i .. " (duplicate of an earlier frame)")
    end
end

spr:close()
