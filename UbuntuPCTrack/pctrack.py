#!/usr/bin/python
# -*- coding: utf-8 -*-

import subprocess
import time
import thread
import codecs
import pickle
import httplib2
import pprint
import sys

import xml.etree.ElementTree as ET
import xml.etree as etree

from os import listdir
from datetime import datetime, timedelta
from apiclient.discovery import build
from apiclient.http import MediaFileUpload
from oauth2client.client import OAuth2WebServerFlow

def get_current_activity():
    focus_window_command = "xprop -root | awk '/_NET_ACTIVE_WINDOW\(WINDOW\)/{print $NF}'"
    focus_window = subprocess.Popen(focus_window_command, stdout=subprocess.PIPE, shell=True)
    (output, err) = focus_window.communicate()
    window = output.strip()
    if (len(window) != 10):
        window = window[:2] + "0" + window[2:]

    focus_id_command = "xprop -id " + window + " | awk '/_NET_WM_PID\(CARDINAL\)/{print $NF}'"
    focus_id = subprocess.Popen(focus_id_command, stdout=subprocess.PIPE, shell=True)
    (output, err) = focus_id.communicate()
    wid = output.strip()

    process_command = "ps -A | grep "+wid
    process_com = subprocess.Popen(process_command, stdout=subprocess.PIPE, shell=True)
    (output, err) = process_com.communicate()
    processm = output.strip()
    if len(processm.split('\n')) < 1:
        return ["",""]
    elif len(processm.split('\n')) > 1:
        for s in processm.split('\n'):
            if s.split()[0] == wid:
                processm = s
                break
    process = ""
    for item in processm.split()[3:]:
        process = process + item + " "
    process = process.strip()

    title_command = "wmctrl -l | grep " + window
    title_com = subprocess.Popen(title_command, stdout=subprocess.PIPE, shell=True)
    (output, err) = title_com.communicate()
    titlem = output.strip()
    if len(titlem.split('\n')) < 1:
        return ["", ""]
    elif len(titlem.split('\n')) > 1:
        for s in titlem.split('\n'):
            if s.split()[0] == window:
                titlem = s
                break
    title = ""
    for item in titlem.split()[3:]:
        title = title + item + " "
    title = title.strip()

    return process, title

def indent(elem, level=0):
    i = "\n" + level*"  "
    if len(elem):
        if not elem.text or not elem.text.strip():
            elem.text = i + "  "
        if not elem.tail or not elem.tail.strip():
            elem.tail = i
        for elem in elem:
            indent(elem, level+1)
        if not elem.tail or not elem.tail.strip():
            elem.tail = i
    else:
        if level and (not elem.tail or not elem.tail.strip()):
            elem.tail = i

def timedelta_from_string(s):
    parts = s.split(".")
    dt = datetime.strptime(parts[0], "%H:%M:%S")
    dt.replace(microsecond=int(parts[1].strip()))
    delta = timedelta(hours=dt.hour, minutes=dt.minute, seconds=dt.second, microseconds=dt.microsecond)
    return delta

def send_screen_change(process, title, period):
    global homedir
    if isinstance(process, unicode) == False:
        process = process.decode("utf-8")
    if isinstance(title, unicode) == False:
        title = title.decode("utf-8")
    config = codecs.open(homedir+"/.pctrack", "r", encoding="utf-8")
    app_dir = config.readline().strip().replace("~", homedir)
    config.close()

    temp_folder_path = app_dir + "/temp"
    dir_command = "mkdir -p " + temp_folder_path
    dir_com = subprocess.Popen(dir_command, stdout=subprocess.PIPE, shell=True)
    (output, err) = dir_com.communicate()

    now = datetime.now()
    new_date_time = None
    if now.minute < 30:
        new_date_time = now.replace(minute=0, second=0, microsecond=0)
    else:
        new_date_time = now.replace(minute=30, second=0, microsecond=0)

    date_time_string = str(new_date_time).replace(":00:00", ":00").strip()
    date_time_string = date_time_string.replace(":30:00", ":30").strip()

    exists = False
    temp_file_name = temp_folder_path + "/" + date_time_string
    for f in listdir(temp_folder_path):
        if f == date_time_string:
            exists = True
            break
    if exists == True:
        try:
            cat_root = ET.parse(temp_file_name)
        except ET.ParseError:
            exists = False
    if exists == False:
        main = ET.Element("catalog", date=date_time_string.split()[0], time=date_time_string.split()[1])
        ET.SubElement(main, "entries")
        indent(main)
        main_tree = ET.ElementTree(main)
        main_tree.write(temp_file_name, encoding='UTF-8', xml_declaration=True, method='xml')
    cat_root = ET.parse(temp_file_name)
    root = cat_root.getroot()
    found_process = False
    for entry in root.find("entries").iter("entry"):
        if entry.attrib["processName"] == process:
            found_process = True
            cur_period = timedelta_from_string(entry.attrib["period"])
            new_period = cur_period + period
            entry.set('period', "0"+str(new_period))
            found_title = False
            for record in entry.iter("record"):
                if record.attrib["title"] == title:
                    found_title = True
                    cur_period = timedelta_from_string(record.attrib["period"])
                    new_period = cur_period + period
                    record.set('period', "0"+str(new_period))
            if found_title == False:
                ET.SubElement(entry, "record", title=title, period="0"+str(period))
    if found_process == False:
        ET.SubElement(root.find("entries"), "entry", processName=process, period="00:00:00.0000000")
        indent(root)
        main_tree = ET.ElementTree(root)
        main_tree.write(temp_file_name, encoding='UTF-8', xml_declaration=True, method='xml')
        return send_screen_change(process, title, period)
    indent(root)
    main_tree = ET.ElementTree(root)
    main_tree.write(temp_file_name, encoding='UTF-8', xml_declaration=True, method='xml')

def monitor_changes():
    global is_idle
    global log_threshold_time
    process, title, timed = "", "", datetime.now()
    is_idle_logged = False
    while True:
        if is_idle == False:
            is_idle_logged = False
            p, t = get_current_activity()
            tn = datetime.now()
            if p != process or t != title:
                if process != "" and title != "":
                    send_screen_change(process, title, tn-timed)
                #print "process:", p, "\n", "title:", t, "\n"
                process = p
                title = t
                timed = tn
            elif (tn-timed).total_seconds() >= log_threshold_time:
                if process != "" and title != "":
                    send_screen_change(process, title, tn-timed)
                timed = tn
        else:
            if is_idle_logged == False:
                tn = datetime.now()
                if process != "" and title != "":
                    send_screen_change(process, title, tn-timed)
                is_idle_logged = True
            timed = datetime.now()
        time.sleep(0.2)

def idle_detector():
    global is_idle
    global idle_time_threshold
    while True:
        idle_com = subprocess.Popen("xprintidle", stdout=subprocess.PIPE, shell=True)
        (output, err) = idle_com.communicate()
        idle_time = int(output.strip())*1.0/1000
        if idle_time > idle_time_threshold:
            is_idle = True
        else:
            is_idle = False
        time.sleep(0.2)

def get_file_id(drive_service, parent_id, file_name):
    global app_dir
    f = open(app_dir+"/id", "r")
    aid = f.readline().strip()
    f.close()
    page_token = None
    while True:
        try:
            param = {}
            if page_token:
                param['pageToken'] = page_token
            children = drive_service.children().list(
                folderId=parent_id, **param).execute()
            for child in children.get('items', []):
                child_id = child['id']
                child_file = drive_service.files().get(fileId=child_id).execute()
                if child_file['title'] == file_name and child_file['description'] == "PCTrack_id "+aid:
                    return child_file
            page_token = children.get('nextPageToken')
            if not page_token:
                break
        except:
            return ""
            break
    return ""

def is_current_file(tfile):
    now = datetime.now()
    new_date_time = None
    if now.minute < 30:
        new_date_time = now.replace(minute=0, second=0, microsecond=0)
    else:
        new_date_time = now.replace(minute=30, second=0, microsecond=0)

    date_time_string = str(new_date_time).replace(":00:00", ":00").strip()
    date_time_string = date_time_string.replace(":30:00", ":30").strip()

    if tfile == date_time_string:
        return True
    return False

def upload_file(tdir, tfile):
    global app_dir
    if is_current_file(tfile):
        return False
    #print "\t\t\t\tuploading", tfile
    f = open(app_dir+"/id", "r")
    aid = f.readline().strip()
    f.close()
    credentials = pickle.load(open(app_dir + "/credentials", "rb"))
    try:
        http = httplib2.Http()
        http = credentials.authorize(http)
        drive_service = build('drive', 'v2', http=http)
        root_id = drive_service.about().get().execute()["rootFolderId"]
        pctrack_id = get_file_id(drive_service, root_id, "PCTrack")
        if pctrack_id == "":
            return False
        temp_id = get_file_id(drive_service, pctrack_id['id'], "Temp")
        # Insert a file
        media_body = MediaFileUpload(tdir+"/"+tfile, mimetype='text/plain', resumable=True)
        body = {
            'title': tfile,
            'description': 'PCTrack_id ' + aid,
            'mimeType': 'text/plain',
            'parents': [temp_id]
        }
        f = drive_service.files().insert(body=body, media_body=media_body).execute()
        #print "\t\t\t\tuploaded", tfile
        #print ""
        return True
    except Exception as e:
        #print "failed to upload", tfile
        #print ""
        return False

def upload_files():
    global app_dir
    temp_dir = app_dir + "/temp"
    while True:
        for f in listdir(temp_dir):
            if f == "sent_files":
                continue
            command = "touch " + temp_dir + "/sent_files"
            sent_file_com = subprocess.Popen(command, stdout=subprocess.PIPE, shell=True)
            (output, err) = sent_file_com.communicate()
            sfile = open(temp_dir + "/sent_files", "r")
            file_sent = False
            for line in sfile:
                if line.strip() == f:
                    file_sent = True
                    break
            sfile.close()
            if file_sent == False:
                sent_success = upload_file(temp_dir, f)
                if sent_success:
                    sfile = open(temp_dir + "/sent_files", "a")
                    sfile.write(f + "\n")
                    sfile.close()
        time.sleep(120)

homedir_command = "echo $HOME"
homedir_com = subprocess.Popen(homedir_command, stdout=subprocess.PIPE, shell=True)
(output, err) = homedir_com.communicate()
homedir = output.strip()

config = codecs.open(homedir+"/.pctrack", "r", encoding="utf-8")
app_dir = config.readline().strip().replace("~", homedir)
config.close()

is_idle = False
idle_time_threshold = 900
log_threshold_time = 29
thread.start_new_thread(idle_detector, ())
thread.start_new_thread(upload_files, ())
monitor_changes()
