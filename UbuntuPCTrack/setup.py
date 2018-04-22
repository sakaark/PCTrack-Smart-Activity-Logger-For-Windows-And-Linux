import subprocess
import httplib2
import pprint
import pickle
import sys

from apiclient.discovery import build
from apiclient.http import MediaFileUpload
from oauth2client.client import OAuth2WebServerFlow

homedir_command = "echo $HOME"
homedir_com = subprocess.Popen(homedir_command, stdout=subprocess.PIPE, shell=True)
(output, err) = homedir_com.communicate()
homedir = output.strip()

print "Enter application directory:",
root = raw_input()
root = root.replace("~", homedir)

dir_command = "mkdir -p " + root
dir_com = subprocess.Popen(dir_command, stdout=subprocess.PIPE, shell=True)
(output, err) = dir_com.communicate()

dir_command = "mkdir -p " + root + "/temp"
dir_com = subprocess.Popen(dir_command, stdout=subprocess.PIPE, shell=True)
(output, err) = dir_com.communicate()

print "This app uses google drive to store all your logs.\n"

CLIENT_ID = '349699183942-1fo1k1b6c464e95cqrjfkn9sgeekklog.apps.googleusercontent.com'
CLIENT_SECRET = '8jJRhXy3oeT5uC8ji2qKxct5'

# Check https://developers.google.com/drive/scopes for all available scopes
OAUTH_SCOPE = 'https://www.googleapis.com/auth/drive'

# Redirect URI for installed apps
REDIRECT_URI = 'urn:ietf:wg:oauth:2.0:oob'

# Path to the file to upload
FILENAME = 'document.txt'

# Run through the OAuth flow and retrieve credentials
flow = OAuth2WebServerFlow(CLIENT_ID, CLIENT_SECRET, OAUTH_SCOPE, REDIRECT_URI)
authorize_url = flow.step1_get_authorize_url()
print 'Go to the following link in your browser, get verification code and paste it here:\n' + authorize_url
code = raw_input('Enter verification code: ').strip()
credentials = flow.step2_exchange(code)
pickle.dump(credentials, open(root+"/credentials", "wb"))
#credentials = pickle.load(open(root+"/credentials", "rb"))

from random import randint
i = randint(0, 999999)

f = open(root+"/id", "w")
f.write(str(i))
f.close()

description = "PCTrack_id "+str(i)

pctrack_id = None

try:
    http = httplib2.Http()
    http = credentials.authorize(http)
    drive_service = build('drive', 'v2', http=http)
    # Insert a file
    body = {
        'title': "PCTrack",
        'mimeType': 'application/vnd.google-apps.folder'
    }
    body['description'] = description
    pctrack_id = drive_service.files().insert(body=body).execute()
except Exception as e:
    print "folder PCTrack cant be made"
    print "\nPlease check your internet connection. Could not complete installation.\n"
    sys.exit()

try:
    credentials = pickle.load(open(root+"/credentials", "rb"))
    http = httplib2.Http()
    http = credentials.authorize(http)
    drive_service = build('drive', 'v2', http=http)
    # Insert a file
    body = {
        'title': "Temp",
        'mimeType': 'application/vnd.google-apps.folder',
        'parents': [pctrack_id]
    }
    body['description'] = description
    f = drive_service.files().insert(body=body).execute()
except:
    print "folder Temp cant be made"
    print "\nPlease check your internet connection. Could not complete installation.\n"
    sys.exit()

try:
    credentials = pickle.load(open(root+"/credentials", "rb"))
    http = httplib2.Http()
    http = credentials.authorize(http)
    drive_service = build('drive', 'v2', http=http)
    media_body = MediaFileUpload(root+"/id", mimetype='text/plain', resumable=True)
    # Insert a file
    title = str(i)
    body = {
        'title': title,
        'description': description,
        'mimeType': 'text/plain',
        'parents': [pctrack_id]
    }
    f = drive_service.files().insert(body=body, media_body=media_body).execute()
except:
    print "Id file cant be made"
    print "\nPlease check your internet connection. Could not complete installation.\n"
    sys.exit()

config = open(homedir+"/.pctrack", "w")
config.write(root)
config.close()

move_file = "cp ./pctrack.py " + root
move_file_com = subprocess.Popen(move_file, stdout=subprocess.PIPE, shell=True)
(output, err) = move_file_com.communicate()

autostart_folder = "~/.config/autostart".replace("~", homedir)
autostart_command = "mkdir -p " + autostart_folder
make_autostart_com = subprocess.Popen(autostart_command, stdout=subprocess.PIPE, shell=True)
(output, err) = make_autostart_com.communicate()

autostart_file = open(autostart_folder + "/pctrack.py.desktop", "w")
autostart_file.write('''[Desktop Entry]
Type=Application
Exec=python '''+root+'''/pctrack.py
Hidden=false
NoDisplay=false
X-GNOME-Autostart-enabled=true
Name[en_IN]=PCTrack
Name=PCTrack
Comment[en_IN]=Send logs to google drive
Comment=Send logs to google drive''')

start_command = "python "+root+"/pctrack.py &"
start_com = subprocess.Popen(start_command, stdout=subprocess.PIPE, shell=True)
#(output, err) = start_com.communicate()

print "Application Installed and started !!"
