import json
import uuid
import datetime
import requests


def obtain_auth_token(api_key):
    url = 'https://westus.api.cognitive.microsoft.com/sts/v1.0/issueToken'
    headers = {
        'Content-type': 'application/x-www-form-urlencoded',
        'Content-Length': '0',
        'Ocp-Apim-Subscription-Key': api_key
    }

    response = requests.post(url, headers=headers)

    # DEBUG PRINT
    # print(r.headers['content-type'])
    # print(r.text)

    if response.status_code == 200:
        data = response.text
        print(data)
    elif response.status_code == 403 or response.status_code == 401:
        print('Error: access denied. Please, make sure you provided a valid API key.')
        exit()
    else:
        response.raise_for_status()

    return data


def generate_id():
    return str(uuid.uuid4()).replace('-', '')


def generate_timestamp():
    return str(datetime.datetime.now()).replace(' ', 'T')[:-3] + 'Z'


def parse_header_value(response, header_to_find):
    for line in response.split('\r\n'):
        if len(line) == 0:
            break
        header_name = header_to_find + ':'
        if line.startswith(header_name):
            return line[len(header_name):].strip()

    return None


def parse_body_json(response):
    body_dict = None

    response_as_lines = response.split('\r\n')
    for i, line in enumerate(response_as_lines):
        if len(line) == 0:
            if i < len(response_as_lines) - 1:
                # load the lines from the header-body separator until the end (corresponding to a JSON) into a dictionary
                try:
                    body_dict = json.loads('\n'.join(response_as_lines[(i + 1):]))
                except json.JSONDecodeError as e:
                    print('JSON Decode Error: {0}.'.format(e))
            break

    return body_dict
