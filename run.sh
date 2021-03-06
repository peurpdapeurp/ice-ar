#!/bin/bash

. ./cleanup.sh

CONSUMER_IMAGE=peetonn/ice-ar:consumer-v3
YOLO_IMAGE=peetonn/ice-ar:yolo-new
OPENFACE_IMAGE=peetonn/ice-ar:openface-new
OPENPOSE_IMAGE=peetonn/ice-ar:openpose
PUBLISHER_IMAGE=peetonn/ice-ar:publisher-new

RAWVIDEO_VOL=rawvideoVol
JSON_VOL=annotationsVol
DB_VOL=/tmp/dbingest

mkdir -p $DB_VOL

case "$1" in
	"consumer")
        docker rm consumer1 2>/dev/null
        docker run --name=consumer1 --rm -v /var/run:/var/run -v $HOME/.ndn:/root/.ndn \
            -v $RAWVIDEO_VOL:/out -v $EDGE_ENV_FOLDER/logs:/tmp -v $EDGE_ENV_FOLDER/preview:/preview -ti \
            -e SIGNING_IDENTITY=/`whoami` $CONSUMER_IMAGE
    	;;
    "yolo")
        docker rm yolo1 2>/dev/null
        docker run --runtime=nvidia --rm --name=yolo1 -v $RAWVIDEO_VOL:/in -v $JSON_VOL:/out \
            -v $EDGE_ENV_FOLDER/preview:/preview -ti $YOLO_IMAGE
        ;;
    "openface")
        docker rm openface1 2>/dev/null
        HAS_OPENFACE_TRAINED=`docker images | grep openface-trained`
        TRAINED_IMAGE="ice-ar:openface-trained"
        if [ -z "${HAS_OPENFACE_TRAINED}" ]; then
            echo "training openface from faces in $(pwd)/edge-env/faces..."
            docker run --runtime=nvidia --name=openface-trained -v `pwd`/edge-env/faces:/faces $OPENFACE_IMAGE /train.sh /faces
            docker commit openface-trained $TRAINED_IMAGE
            docker rm openface-trained
            docker rmi $OPENFACE_IMAGE
        fi
        docker run --runtime=nvidia --rm --name=openface1 -v $RAWVIDEO_VOL:/in -v $JSON_VOL:/out \
            -v $EDGE_ENV_FOLDER/preview:/preview -it \
            $TRAINED_IMAGE /run.sh
        ;;
    "openpose")
        docker rm openpose1 2>/dev/null
        docker run --runtime=nvidia --rm --name=openpose1 -v $RAWVIDEO_VOL:/in -v $JSON_VOL:/out \
            -v $EDGE_ENV_FOLDER/preview:/preview -ti \
            $OPENPOSE_IMAGE
        ;;
    "publisher")
        docker rm publisher 2>/dev/null
        docker run --name publisher --rm -v /var/run:/var/run -v $HOME/.ndn:/root/.ndn -v $JSON_VOL:/in -v $DB_VOL:/out -ti \
        $PUBLISHER_IMAGE
        echo $cmd
        ;;
    "db")
        python edge/semantic-db/nanoreader.py $DB_VOL/ice-annotations &
        python edge/semantic-db/tornado/run.py
        ;;
    *)
        echo "> unknown argument "$1
        ;;
esac
