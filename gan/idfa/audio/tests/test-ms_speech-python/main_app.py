import client as client
import asyncio
import os


async def main():
    #audio_file_path = 'data/whatstheweatherlike.wav'
    audio_file_path = 'data/tony_robbins_1.wav'
    key = os.environ["MICROSOFT_KEY"]
    language = "en-US"
    
    recognitionMode = "conversation"
    
    formatOptions = "Simple"
    inputSource = "File"
    phrase = await client.start(
        key,
        language,
        formatOptions,
        recognitionMode,
        audio_file_path
    )

    print("Phrase : {}".format(phrase))


if __name__ == '__main__':
    asyncio.run(main())


