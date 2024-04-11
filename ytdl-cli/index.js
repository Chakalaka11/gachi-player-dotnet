const { Command } = require('commander');
const ytdl = require('ytdl-core');
const util = require('util');
const stream = require('stream');
const fs = require('fs');

const pipeline = util.promisify(stream.pipeline);

const options = { quality: 'highestaudio' };
const supportedAudioFormats = ['mp4', 'mp3'];
const defaultFolder = './AudioFiles'

const downloadVideo = async (videoUrl, folderPath) => {

    let folderToSave = folderPath ? folderPath : defaultFolder;
    let info = await ytdl.getInfo(videoUrl, options);
    let audioFormats = ytdl.filterFormats(info.formats, 'audioonly');

    for (let i = 0; i < supportedAudioFormats.length; i++) {
        if(fs.existsSync(`${folderToSave}/audioTrack-${info.videoDetails.videoId}.${supportedAudioFormats[i]}`))
        {
            console.log('[WARNING] File was already downloaded, exiting...');
            console.log(`[WARNING] Saved at ${folderToSave}/audioTrack-${info.videoDetails.videoId}.${supportedAudioFormats[i]}`);
            return;
        }
    }

    console.log('[INFO] Trying to download media...');

    for (let i = 0; i < supportedAudioFormats.length; i++) {
        const format = supportedAudioFormats[i];
        let supportedFormat = audioFormats.find(x => x.container === format);
        if (supportedFormat) {
            console.log(`[INFO] Found supported format - ${format}`);

            if (!fs.existsSync(folderToSave)) {
                fs.mkdirSync(folderToSave);
            }
            let filePath = `${folderToSave}/audioTrack-${info.videoDetails.videoId}.${format}`;

            console.log(`[INFO] Saved at ${filePath}`);

            // I DON'T HAVE SINGLE FUCKING IDEA HOW THAT WORKED
            await pipeline(
                ytdl.downloadFromInfo(info, { mimeType: supportedFormat.mimeType, filter: 'audioonly' }),
                fs.createWriteStream(filePath));

            console.log('[INFO] Download complete!');
            return filePath;
        }
    }

    console.log('[WARNING] No supported options availabe, exiting...');
    return null;
};

const program = new Command();
program
    .version('0.0.1')
    .description('Youtube CLI downloader')
    .argument('<youtubeLink>')
    .argument('[folderPath]')
    .action(async (youtubeLink, folderPath) => await download(youtubeLink, folderPath));

async function download(youtubeLink, folderPath) {
    await downloadVideo(youtubeLink, folderPath);
}
program.parse();