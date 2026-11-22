for c in double_saw lash lamp_tear; do python3 ../../../pixellab_call.py /v2/animate-with-skeleton-v3 body_$c.json resp_$c.json > run2_$c.out 2>&1; done
