INSERT INTO public.users (username, status, age_range, catchphrase)
VALUES ('supabot', 'ONLINE', '[1,2)'::int4range, 'fat cat'::tsvector),
       ('kiwicopple', 'OFFLINE', '[25,35)'::int4range, 'cat bat'::tsvector),
       ('awailas', 'ONLINE', '[25,35)'::int4range, 'bat rat'::tsvector),
       ('dragarcia', 'ONLINE', '[20,30)'::int4range, 'rat fat'::tsvector);

INSERT INTO public.channels (slug)
VALUES ('public'),
       ('random');

INSERT INTO public.messages (message, channel_id, username)
VALUES ('Hello World 👋', 1, 'supabot'),
       ('Perfection is attained, not when there is nothing more to add, but when there is nothing left to take away.',
        2, 'supabot');

INSERT INTO personal.users (username, status, age_range)
VALUES ('supabot', 'ONLINE', '[1,2)'::int4range),
       ('kiwicopple', 'OFFLINE', '[25,35)'::int4range),
       ('awailas', 'ONLINE', '[25,35)'::int4range),
       ('dragarcia', 'ONLINE', '[20,30)'::int4range),
       ('leroyjenkins', 'ONLINE', '[20,40)'::int4range);

INSERT INTO public.kitchen_sink (string_value,
                                 int_value,
                                 float_value,
                                 double_value,
                                 datetime_value,
                                 datetime_value_1,
                                 datetime_pos_infinite_value,
                                 datetime_neg_infinite_value,
                                 list_of_strings,
                                 list_of_datetimes,
                                 list_of_ints,
                                 list_of_floats,
                                 int_range)
VALUES ('Im the Kitchen Sink!',
        99999,
        '99999.0'::float4,
        '99999.0'::float8,
        'Tue May 24 06:30:00 2022'::timestamp,
        'Tue May 20 06:00:00 2022'::timestamp,
        'Infinity',
        '-infinity',
        '{"set", "of", "strings"}',
        '{NOW()}',
        '{10, 20, 30, 40}',
        '{10.0, 12.0}',
        '[20,50]'::int4range);