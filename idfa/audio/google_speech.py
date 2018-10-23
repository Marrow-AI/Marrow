from google.cloud import texttospeech

class GoogleSpeech:

    def __init__(self):
        self.client = texttospeech.TextToSpeechClient()

    def say(self, text):
        print("Google Say {}".format(text))
        synthesis_input = texttospeech.types.SynthesisInput(text=text)

        voice = texttospeech.types.VoiceSelectionParams(
            name='en-US-Wavenet-C',
            language_code='en-US'
        )

        audio_config = texttospeech.types.AudioConfig(
            audio_encoding=texttospeech.enums.AudioEncoding.LINEAR16,
            pitch=-2.8,
            speaking_rate = 0.8
        )

        print("Synthesizing")
        response = self.client.synthesize_speech(synthesis_input, voice, audio_config)
        print("Done")

        with open('/tmp/love.wav', 'wb') as fd:
            fd.write(response.audio_content)
            print("Wrote to file")

    def list_voices(self):
        """Lists the available voices."""

        # Performs the list voices request
        voices = self.client.list_voices()

        for voice in voices.voices:
            # Display the voice's name. Example: tpc-vocoded
            print('Name: {}'.format(voice.name))

            # Display the supported language codes for this voice. Example: "en-US"
            for language_code in voice.language_codes:
                print('Supported language: {}'.format(language_code))

            # SSML Voice Gender values from google.cloud.texttospeech.enums
            ssml_voice_genders = ['SSML_VOICE_GENDER_UNSPECIFIED', 'MALE',
                                  'FEMALE', 'NEUTRAL']

            # Display the SSML Voice Gender
            print('SSML Voice Gender: {}'.format(
                ssml_voice_genders[voice.ssml_gender]))

            # Display the natural sample rate hertz for this voice. Example: 24000
            print('Natural Sample Rate Hertz: {}\n'.format(
                voice.natural_sample_rate_hertz))




