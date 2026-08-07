INSERT INTO public.users (username, status, age_range, catchphrase)
VALUES ('supabot', 'ONLINE', '[1,2)'::int4range, 'fat cat'::tsvector),
       ('kiwicopple', 'OFFLINE', '[25,35)'::int4range, 'cat bat'::tsvector),
       ('awailas', 'ONLINE', '[25,35)'::int4range, 'bat rat'::tsvector),
       ('acupofjose', 'OFFLINE', '[25,35)'::int4range, 'bat rat'::tsvector),
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
       ('leroyjenkins', 'OFFLINE', '[20,40)'::int4range);

INSERT INTO public.kitchen_sink (id,
                                 string_value,
                                 bool_value,
                                 int_value,
                                 long_value,
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

VALUES ('f3ff356d-5803-43a7-b125-ba10cf10fdcd',
        'Im the Kitchen Sink!',
        false,
        99999,
        2147483648,
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


insert into "public"."movie" ("created_at", "id", "name", "status")
values ('2022-08-20 00:29:45.400188', 'ea07bd86-a507-4c68-9545-b848bfe74c90', 'Top Gun: Maverick', 'OnDisplay');
insert into "public"."movie" ("created_at", "id", "name", "status")
values ('2022-08-22 00:29:45.400188', 'a972a8f6-2e23-4172-be8d-7b65470ca0f4', 'Mad Max: Fury Road', 'OnDisplay');
insert into "public"."movie" ("created_at", "id", "name", "status")
values ('2022-08-28 00:29:45.400188', '42fd15b1-3bff-431d-9fa5-314289beb246', 'Guns Away', 'OffDisplay');


insert into "public"."person" ("created_at", "first_name", "id", "last_name")
values ('2022-08-20 00:30:02.120528', 'Tom', 'd53072eb-5e64-4e9c-8a29-3ed07076fb2f', 'Cruise');
insert into "public"."person" ("created_at", "first_name", "id", "last_name")
values ('2022-08-20 00:30:02.120528', 'Tom', 'b76776ac-75ba-424f-b5bc-6cb85c2d2bbf', 'Holland');
insert into "public"."person" ("created_at", "first_name", "id", "last_name")
values ('2022-08-20 00:30:33.72443', 'Bob', '6f06c038-38e0-4a39-8aac-2c5e8597856e', 'Saggett');
insert into "public"."person" ("created_at", "first_name", "id", "last_name")
values ('2022-08-20 00:30:33.72443', 'Random', 'd948ca02-c432-470e-9fe5-738269491762', 'Actor');


insert into "public"."profile" ("created_at", "email", "person_id")
values ('2022-08-20 00:30:33.72443', 'tom.cruise@supabase.io', 'd53072eb-5e64-4e9c-8a29-3ed07076fb2f');
insert into "public"."profile" ("created_at", "email", "person_id")
values ('2022-08-20 00:30:33.72443', 'tom.holland@supabase.io', 'b76776ac-75ba-424f-b5bc-6cb85c2d2bbf');
insert into "public"."profile" ("created_at", "email", "person_id")
values ('2022-08-20 00:30:33.72443', 'bob.saggett@supabase.io', '6f06c038-38e0-4a39-8aac-2c5e8597856e');

insert into "public"."movie_person" ("movie_id", "person_id")
values ('ea07bd86-a507-4c68-9545-b848bfe74c90', 'd53072eb-5e64-4e9c-8a29-3ed07076fb2f');
insert into "public"."movie_person" ("movie_id", "person_id")
values ('a972a8f6-2e23-4172-be8d-7b65470ca0f4', 'b76776ac-75ba-424f-b5bc-6cb85c2d2bbf');
insert into "public"."movie_person" ("movie_id", "person_id")
values ('ea07bd86-a507-4c68-9545-b848bfe74c90', '6f06c038-38e0-4a39-8aac-2c5e8597856e');
insert into "public"."movie_person" ("movie_id", "person_id")
values ('42fd15b1-3bff-431d-9fa5-314289beb246', 'd948ca02-c432-470e-9fe5-738269491762');

insert into "public"."foreign_key_test" ("movie_fk_1", "movie_fk_2", "random_person_fk")
values ('ea07bd86-a507-4c68-9545-b848bfe74c90', 'ea07bd86-a507-4c68-9545-b848bfe74c90',
        'd53072eb-5e64-4e9c-8a29-3ed07076fb2f');

insert into "public"."nested_foreign_key_test" ("foreign_key_test_fk", "user_fk")
values ('1', 'awailas');

insert into "public"."category" ("id", "name")
values ('999e4b26-91a8-4ea4-af2c-77a3540f7843', 'category 1'),
       ('f15a224d-014d-4c21-a733-e2d0862eafe1', 'category 2');

insert into "public"."product" ("id", "name", "category_id")
values ('8b8e89a0-63c7-4917-8dc1-7797dc0285f1', 'product 1', '999e4b26-91a8-4ea4-af2c-77a3540f7843'),
       ('db418e89-d472-415f-9bf1-7ae713d83617', 'product 2', '999e4b26-91a8-4ea4-af2c-77a3540f7843'),
       ('f98aadbe-5796-44f8-be64-3832424940d4', 'product 3', 'f15a224d-014d-4c21-a733-e2d0862eafe1');
