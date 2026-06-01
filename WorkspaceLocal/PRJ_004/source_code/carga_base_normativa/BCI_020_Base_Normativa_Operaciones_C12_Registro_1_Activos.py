# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_020_Base_Normativa_Operaciones_C12_Registro_1_Activos
# MAGIC **************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ## Información del Notebook

# COMMAND ----------

# MAGIC %md
# MAGIC ### Encabezado
# MAGIC ***************************************************************************************************************
# MAGIC * Nombre: BCI_020_Base_Normativa_Operaciones_C12_Registro_1_Activos.ipynb
# MAGIC * Ruta: 
# MAGIC * Autor: BitSolucion Spa
# MAGIC * Ing. SW BCI: <alguien.de.ti@bci.cl> - Marcelo Lizama <marcelo.lizama@bci.cl> - Ivan Jara <ivan.jara@bci.cl>
# MAGIC * Fecha: 22/12/2025
# MAGIC * Descripción: Notebook encargado de generar la informacion de las operaciones activas C12 registro 1
# MAGIC * Documentación: 
# MAGIC ***************************************************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Mantenciones
# MAGIC **************************************************************************
# MAGIC #### Mantención Nro: 
# MAGIC * Autor: <Nombre Autor> (<Empresa del Autor (Bci/Otra)>) - Ing. SW BCI: <Nombre Ing. SW BCI>
# MAGIC * Fecha: <dd/mm/yyyy> 
# MAGIC * Descripción: <Descripción de la mantención>      
# MAGIC ***************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tablas Entrada y Salida
# MAGIC **************************************************************************
# MAGIC #### Tablas Entrada: 
# MAGIC * v2_tabmae_mapeo_normativo
# MAGIC * TBU
# MAGIC * tmp_d00_v3_act_ctg
# MAGIC * tmp_arm_d00_v3_act_3
# MAGIC ***************************************************************************
# MAGIC #### Tablas Salida: 
# MAGIC * base_normativa_v2_ft

# COMMAND ----------

# MAGIC %md
# MAGIC ## Captura de Variable

# COMMAND ----------

# MAGIC %md
# MAGIC ### Crear Widgets para Capturar de Variables

# COMMAND ----------

dbutils.widgets.removeAll()

dbutils.widgets.text("fechaProcesoW","","01 Fecha Proceso:")
dbutils.widgets.text("db_platinumW","","02 platinum DB:")
dbutils.widgets.text("db_plat_tempW","","03 platinum temp db:")
dbutils.widgets.text("db_RiesgoCredW","","04 slv_RiesgoCred_RiesgoCredPer DB:")
dbutils.widgets.text("db_slv_ParametricasW","","05 Parametricas DB:")

dbutils.widgets.text("db_location_plat_tempW","","06 platinum temp db:")

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignar Objeto a Lectura de Widgets y Variables

# COMMAND ----------

fechaProcesoX = dbutils.widgets.get("fechaProcesoW")
spark.conf.set("bci.fechaProcesoX", fechaProcesoX)

db_platinumX = dbutils.widgets.get("db_platinumW")
spark.conf.set("bci.db_platinumX", db_platinumX)

db_plat_tempX = dbutils.widgets.get("db_plat_tempW")
spark.conf.set("bci.db_plat_tempX", db_plat_tempX)

db_RiesgoCredX = dbutils.widgets.get("db_RiesgoCredW")
spark.conf.set("bci.db_RiesgoCredX", db_RiesgoCredX)

db_slv_ParametricasX = dbutils.widgets.get("db_slv_ParametricasW")
spark.conf.set("bci.db_slv_ParametricasX", db_slv_ParametricasX)


db_location_plat_tempX = dbutils.widgets.get("db_location_plat_tempW")
spark.conf.set("bci.db_location_plat_tempX", db_location_plat_tempX)


print("Parámetros")
print("fechaProcesoX: " + fechaProcesoX)
print("db_platinumX: " + db_platinumX)
print("db_plat_tempX: " + db_plat_tempX)
print("db_RiesgoCredX: " + db_RiesgoCredX)
print("db_slv_ParametricasX: " + db_slv_ParametricasX)
print("db_location_plat_tempX: " + db_location_plat_tempX)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Carga de funciones

# COMMAND ----------

# MAGIC %run "../../Funciones"

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validación de ingreso de parámetros

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Parametro Vacio

# COMMAND ----------

valida_param_vacio(fechaProcesoX,'fechaProcesoX')
valida_param_vacio(db_platinumX,'db_platinumX')
valida_param_vacio(db_plat_tempX,'db_plat_tempX')
valida_param_vacio(db_RiesgoCredX,'db_RiesgoCredX')
valida_param_vacio(db_slv_ParametricasX,'db_slv_ParametricasX')
valida_param_vacio(db_location_plat_tempX,'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Base de Datos

# COMMAND ----------

valida_bd(db_platinumX, 'db_platinumX')
valida_bd(db_plat_tempX, 'db_plat_tempX')
valida_bd(db_RiesgoCredX, 'db_RiesgoCredX')
valida_bd(db_slv_ParametricasX, 'db_slv_ParametricasX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Fecha Valida

# COMMAND ----------

valida_fecha_valida(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Fecha Futura

# COMMAND ----------

valida_fecha_futura(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Ruta 

# COMMAND ----------

valida_ruta(db_location_plat_tempX, 'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignacion Variables de fecha

# COMMAND ----------

ano = str(fechaProcesoX)[:4]
mes = str(fechaProcesoX)[4:][:2]
dia = str(fechaProcesoX)[6:][:2]
fechanormativo = str(ano+'-'+mes+'-'+dia)
fechacinta = str(dia+'-'+mes+'-'+ano)
anomes = str(ano+mes)
anomesdia = str(ano+mes+dia)
ano_mes_dia = str(ano+'-'+mes+'-01')

print("fecha_Formato1: " + ano)
print("fecha_Formato2: " + mes)
print("fecha_Formato3: " + dia)
print("fecha_Formato4: " + fechanormativo)
print("fecha_Formato5: " + fechacinta)
print("fecha_Formato6: " + anomes)
print("fecha_Formato7: " + anomesdia)
print("fecha_Formato8: " + ano_mes_dia)

# COMMAND ----------

ult_dia_mes = ultimo_dia_habil(db_slv_ParametricasX, anomes)

ult_dia = ult_dia_mes.toPandas()
fec_proceso = ult_dia.iloc[0]["periodo_id"]

ano_proceso = str(fec_proceso)[:4]
mes_proceso = str(fec_proceso)[4:][:2]
dia_proceso = str(fec_proceso)[6:][:2]
date_proceso = str(ano_proceso+'-'+mes_proceso+'-'+dia_proceso)
date_proceso_2 = str(ano_proceso+mes_proceso+dia_proceso)

print("fecha_Formato9: " + date_proceso)
print("fecha_Formato10: " + date_proceso_2)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Inicio de Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ### Creacion Tablas Temporales

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla base_normativa_v2_ft

# COMMAND ----------

drop_base_normativa_v2_ft = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.base_normativa_v2_ft"""

# COMMAND ----------

sql_safe(drop_base_normativa_v2_ft)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"base_normativa_v2_ft", True)

# COMMAND ----------

create_base_normativa_v2_ft = """CREATE TABLE IF NOT EXISTS """ + db_plat_tempX + """.base_normativa_v2_ft
(
  periodo_id 							INT,
  num_registro            INT,
  cod_categoria           STRING,
  num_tipo_activo         INT,
  cod_activo 							INT,
  rut_cliente 						INT,
  dv_cliente 							STRING,
  cod_num_operacion 			STRING,
  cod_ope_original        STRING,
  cod_cta_contable        STRING,
  fec_proceso 						DATE
) 
USING DELTA
PARTITIONED BY(fec_proceso)
LOCATION '""" + db_location_plat_tempX + """base_normativa_v2_ft'  """

# COMMAND ----------

sql_safe(create_base_normativa_v2_ft)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci20_basenorma_tmp_cta_ain_1

# COMMAND ----------

drop_cta_ain_1 = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_1"""

# COMMAND ----------

sqlsafe(drop_cta_ain_1)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci20_basenorma_tmp_cta_ain_1", True)

# COMMAND ----------

create_cta_ain_1 = """CREATE TABLE """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_1
(
  periodo_id                      INT,
  registro_id                     INT,
  mapeo_norma_id                  INT,				
  cta_ctable                      STRING
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci20_basenorma_tmp_cta_ain_1' """

# COMMAND ----------

sqlsafe(create_cta_ain_1)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci20_basenorma_tmp_c12_reg1

# COMMAND ----------

drop_c12_reg1 = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci20_basenorma_tmp_c12_reg1"""

# COMMAND ----------

sqlsafe(drop_c12_reg1)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci20_basenorma_tmp_c12_reg1", True)

# COMMAND ----------

create_c12_reg1 = """CREATE TABLE """ + db_plat_tempX + """.bci20_basenorma_tmp_c12_reg1
(
  periodo_id            INT,
  registro_id           INT,
  categoria             STRING,
  tipo_activo           INT,
  activo_id             INT,				
  cliente_rut           INT,
  cliente_dv            STRING,
  cliente_completo      STRING,
  ope_vigente           STRING,
  ope_original          STRING,
  cod_cctb              STRING,
  cod_cartera_ope       STRING
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci20_basenorma_tmp_c12_reg1' """

# COMMAND ----------

sqlsafe(create_c12_reg1)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci20_basenorma_tmp_cta_ain_2

# COMMAND ----------

drop_cta_ain_2 = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_2"""

# COMMAND ----------

sqlsafe(drop_cta_ain_2)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci20_basenorma_tmp_cta_ain_2", True)

# COMMAND ----------

create_cta_ain_2 = """CREATE TABLE """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_2
(
  periodo_id                      INT,
  registro_id                     INT,
  mapeo_norma_id                  INT,				
  cta_ctable                      STRING
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci20_basenorma_tmp_cta_ain_2' """

# COMMAND ----------

sqlsafe(create_cta_ain_2)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci20_basenorma_tmp_cta_ain_5

# COMMAND ----------

drop_cta_ain_5 = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_5"""

# COMMAND ----------

sqlsafe(drop_cta_ain_5)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci20_basenorma_tmp_cta_ain_5", True)

# COMMAND ----------

create_cta_ain_5 = """CREATE TABLE """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_5
(
  periodo_id                      INT,
  registro_id                     INT,
  mapeo_norma_id                  INT,				
  cta_ctable                      STRING
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci20_basenorma_tmp_cta_ain_5' """

# COMMAND ----------

sqlsafe(create_cta_ain_5)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci20_basenorma_tmp_cta_ain_6

# COMMAND ----------

drop_cta_ain_6 = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_6"""

# COMMAND ----------

sqlsafe(drop_cta_ain_6)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci20_basenorma_tmp_cta_ain_6", True)

# COMMAND ----------

create_cta_ain_6 = """CREATE TABLE """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_6
(
  periodo_id                      INT,
  registro_id                     INT,
  mapeo_norma_id                  INT,				
  cta_ctable                      STRING
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci20_basenorma_tmp_cta_ain_6' """

# COMMAND ----------

sqlsafe(create_cta_ain_6)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci20_basenorma_tmp_cta_ain_7

# COMMAND ----------

drop_cta_ain_7 = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_7"""

# COMMAND ----------

sqlsafe(drop_cta_ain_7)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci20_basenorma_tmp_cta_ain_7", True)

# COMMAND ----------

create_cta_ain_7 = """CREATE TABLE """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_7
(
  periodo_id                      INT,
  registro_id                     INT,
  mapeo_norma_id                  INT,				
  cta_ctable                      STRING
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci20_basenorma_tmp_cta_ain_7' """

# COMMAND ----------

sqlsafe(create_cta_ain_7)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci20_basenorma_tmp_cta_ain_8

# COMMAND ----------

drop_cta_ain_8 = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_8"""

# COMMAND ----------

sqlsafe(drop_cta_ain_8)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci20_basenorma_tmp_cta_ain_8", True)

# COMMAND ----------

create_cta_ain_8 = """CREATE TABLE """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_8
(
  periodo_id                      INT,
  registro_id                     INT,
  mapeo_norma_id                  INT,				
  cta_ctable                      STRING
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci20_basenorma_tmp_cta_ain_8' """

# COMMAND ----------

sqlsafe(create_cta_ain_8)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci20_basenorma_tmp_cta_ain_9

# COMMAND ----------

drop_cta_ain_9 = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_9"""

# COMMAND ----------

sqlsafe(drop_cta_ain_9)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci20_basenorma_tmp_cta_ain_9", True)

# COMMAND ----------

create_cta_ain_9 = """CREATE TABLE """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_9
(
  periodo_id                      INT,
  registro_id                     INT,
  mapeo_norma_id                  INT,				
  cta_ctable                      STRING
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci20_basenorma_tmp_cta_ain_9' """

# COMMAND ----------

sqlsafe(create_cta_ain_9)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci20_basenorma_tmp_cta_ain_20

# COMMAND ----------

drop_cta_ain_20 = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_20"""

# COMMAND ----------

sqlsafe(drop_cta_ain_20)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci20_basenorma_tmp_cta_ain_20", True)

# COMMAND ----------

create_cta_ain_20 = """CREATE TABLE """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_20
(
  periodo_id                      INT,
  registro_id                     INT,
  mapeo_norma_id                  INT,				
  cta_ctable                      STRING
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci20_basenorma_tmp_cta_ain_20' """

# COMMAND ----------

sqlsafe(create_cta_ain_20)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci20_basenorma_tmp_tipo_ope_21

# COMMAND ----------

drop_tipo_ope_21 = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci20_basenorma_tmp_tipo_ope_21"""

# COMMAND ----------

sqlsafe(drop_tipo_ope_21)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci20_basenorma_tmp_tipo_ope_21", True)

# COMMAND ----------

create_tipo_ope_21 = """CREATE TABLE """ + db_plat_tempX + """.bci20_basenorma_tmp_tipo_ope_21
(
  periodo_id            INT,
  activo_id             INT,				
  cliente_rut           INT,
  cliente_dv            STRING,
  ope_vigente           STRING,
  ope_original          STRING,
  cod_cctb              STRING,
  cod_cartera_ope       STRING,
  cod_tipo_ope          STRING
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci20_basenorma_tmp_tipo_ope_21' """

# COMMAND ----------

sqlsafe(create_tipo_ope_21)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Desarrollo Logica

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 1: Obtencion Univ Tipo Activo 1

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de los activos 1 desde v2_tabmae_mapeo_normativo y TBU
# MAGIC
# MAGIC **Tabla de salida**: bci20_basenorma_tmp_cta_ain_1 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 	
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **mapeo_norma_id**: Numero del mapeo de la norma
# MAGIC - **cta_ctable**: Cuenta contable

# COMMAND ----------

vista_cta_ain_1 = """ insert into """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_1
  select  substring(b.fecha_informada, 1, 6)    as periodo_id,
          a.registro_id,
          a.mapeo_norma_id, 				
          b.cta_ctable
  from """ + db_platinumX + """.tabmae_mapeo_normativo a
  join """ + db_slv_ParametricasX + """.TBU b
    on trim(a.cod_cta_contable) = trim(b.cta_cmf)
  where b.fecha_informada = '""" + date_proceso_2 + """'
    and a.mapeo_norma_id = 1211
    and substring(trim(b.cta_ctable), -1, 1) != 'C'
"""
 

# COMMAND ----------

sql_safe(vista_cta_ain_1)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 2: Obtencion Operaciones Tipo Activo 1

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones y clientes del tipo activo 1 desde tmp_d00_v3_act_ctg y bci20_basenorma_tmp_cta_ain_1
# MAGIC
# MAGIC **Tabla de salida**: bci20_basenorma_tmp_c12_reg1 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **activo_id**: Codigo tipo activo	
# MAGIC - **cliente_rut**: Rut del cliente
# MAGIC - **cliente_dv**: Digito verificador del cliente
# MAGIC - **ope_vigente**: Codigo operacion vigente (cod_num_operacion)
# MAGIC - **ope_original**: Codigo operacion original (cod_ope_original)
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cartera_ope**: Codigo tipo de cartera operacional

# COMMAND ----------

vista_activo_1 = """ insert into """ + db_plat_tempX + """.bci20_basenorma_tmp_c12_reg1
select  a.periodo_id
    ,b.registro_id
    ,'C12'                                      as categoria
    ,1                                          as tipo_activo
    ,1211                                       as activo_id
    ,a.rut_cliente                              as cliente_rut
    ,a.dv_cliente                               as cliente_dv
    ,concat(a.rut_cliente, trim(a.dv_cliente))  as cliente_completo
    ,a.cod_num_operacion                        as ope_vigente
    ,a.cod_ope_original                         as ope_original
    ,a.cod_cctb
    ,a.cod_cartera_ope
from """ + db_plat_tempX + """.tmp_d00_v3_act_ctg a
inner  join """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_1 b
    on trim(a.cod_cctb) = trim(b.cta_ctable)
    and a.periodo_id = b.periodo_id
where a.periodo_id = """ + anomes + """
    and trim(a.cod_cartera_ope) = 'ACT'
"""

# COMMAND ----------

sqlsafe(vista_activo_1)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 3: Obtencion Univ Tipo Activo 2

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de los activos 2 desde v2_tabmae_mapeo_normativo y TBU
# MAGIC
# MAGIC **Tabla de salida**: bci20_basenorma_tmp_cta_ain_2 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 
# MAGIC - **registro_id**: Numero del tipo de registro			
# MAGIC - **mapeo_norma_id**: Numero del mapeo de la norma
# MAGIC - **cta_ctable**: Cuenta contable

# COMMAND ----------

vista_cta_ain_2 = """ insert into """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_2
select  substring(b.fecha_informada, 1, 6)    as periodo_id
      ,a.registro_id
      ,a.mapeo_norma_id
      ,b.cta_ctable
from """ + db_platinumX + """.tabmae_mapeo_normativo a
inner  join """ + db_slv_ParametricasX + """.TBU b
    on trim(a.cod_cta_contable) = trim(b.cta_cmf)
where b.fecha_informada = '""" + date_proceso_2 + """'
    and a.mapeo_norma_id = 1212
    and trim(a.cod_considera_cta) = '1'
    and trim(b.cta_ctable) not in (select trim(cod_cta_contable) as cod_cta_contable
                                  from """ + db_platinumX + """.tabmae_mapeo_normativo 
                                  where mapeo_norma_id = 1212 and trim(cod_considera_cta) = '-1' 
                                  and trim(cod_cta_contable) <> '0')
    and substring(trim(b.cta_ctable), -1, 1) <> 'C'
"""
  

# COMMAND ----------

sql_safe(vista_cta_ain_2)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 4: Obtencion Operaciones Tipo Activo 2

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones y clientes del tipo activo 2 desde tmp_d00_v3_act_ctg y bci20_basenorma_tmp_cta_ain_2
# MAGIC
# MAGIC **Tabla de salida**: bci20_basenorma_tmp_c12_reg1 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **activo_id**: Codigo tipo activo	
# MAGIC - **cliente_rut**: Rut del cliente
# MAGIC - **cliente_dv**: Digito verificador del cliente
# MAGIC - **ope_vigente**: Codigo operacion vigente (cod_num_operacion)
# MAGIC - **ope_original**: Codigo operacion original (cod_ope_original)
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cartera_ope**: Codigo tipo de cartera operacional

# COMMAND ----------

vista_activo_2 = """ insert into """ + db_plat_tempX + """.bci20_basenorma_tmp_c12_reg1
  select  a.periodo_id,
          b.registro_id,
          'C12'                 as categoria,
          2                     as tipo_activo,
          1212                  as activo_id,
          a.rut_cliente         as cliente_rut,	
          a.dv_cliente          as cliente_dv,
          concat(a.rut_cliente, a.dv_cliente)   as cliente_completo,			
          a.cod_num_operacion   as ope_vigente,
          a.cod_ope_original    as ope_original,
          a.cod_cctb,
          a.cod_cartera_ope
  from """ + db_plat_tempX + """.tmp_d00_v3_act_ctg a
  join """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_2 b
    on trim(a.cod_cctb) = trim(b.cta_ctable)
      and a.periodo_id = b.periodo_id
  where a.periodo_id = '""" + anomes + """'
    and trim(a.cod_cartera_ope) = 'ACT'
    and trim(a.cod_tipo_ope) not in ('PAP325', 'PAP328', 'PAP322', 'CON676')
    and trim(a.cod_num_operacion) not in (select trim(cod_num_operacion) from """ + db_plat_tempX + """.tmp_arm_d00_v3_act_3)
"""

# COMMAND ----------

sqlsafe(vista_activo_2)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 5: Obtencion Univ Tipo Activo 5

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de los activos 5 desde v2_tabmae_mapeo_normativo y TBU
# MAGIC
# MAGIC **Tabla de salida**: bci20_basenorma_tmp_cta_ain_5 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 	
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **mapeo_norma_id**: Numero del mapeo de la norma
# MAGIC - **cta_ctable**: Cuenta contable

# COMMAND ----------

vista_cta_ain_5 = """ insert into """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_5
  select  substring(b.fecha_informada, 1, 6)    as periodo_id,
          a.registro_id,
          a.mapeo_norma_id, 				
          b.cta_ctable
  from """ + db_platinumX + """.tabmae_mapeo_normativo a
  join """ + db_slv_ParametricasX + """.TBU b
    on trim(a.cod_cta_contable) = trim(b.cta_cmf)
  where b.fecha_informada = '""" + date_proceso_2 + """'
    and a.mapeo_norma_id = 1215
    and trim(a.cod_considera_cta) = '1'
    and trim(b.cta_ctable) not in (select trim(cod_cta_contable) as cod_cta_contable
                                   from """ + db_platinumX + """.tabmae_mapeo_normativo 
                                   where mapeo_norma_id = 1215 and trim(cod_considera_cta) = '-1' and trim(cod_cta_contable) != '0')
    and substring(trim(b.cta_ctable), -1, 1) != 'C'
"""


# COMMAND ----------

sql_safe(vista_cta_ain_5)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 6: Obtencion Operaciones Tipo Activo 5

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones y clientes del tipo activo 5 desde tmp_d00_v3_act_ctg y bci20_basenorma_tmp_cta_ain_5
# MAGIC
# MAGIC **Tabla de salida**: bci20_basenorma_tmp_c12_reg1 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **activo_id**: Codigo tipo activo	
# MAGIC - **cliente_rut**: Rut del cliente
# MAGIC - **cliente_dv**: Digito verificador del cliente
# MAGIC - **ope_vigente**: Codigo operacion vigente (cod_num_operacion)
# MAGIC - **ope_original**: Codigo operacion original (cod_ope_original)
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cartera_ope**: Codigo tipo de cartera operacional

# COMMAND ----------

vista_activo_5 = """ insert into """ + db_plat_tempX + """.bci20_basenorma_tmp_c12_reg1
  select  a.periodo_id,
          b.registro_id,
          'C12'                 as categoria,
          5                     as tipo_activo,
          1215                  as activo_id,
          a.rut_cliente         as cliente_rut,	
          a.dv_cliente          as cliente_dv,
          concat(a.rut_cliente, a.dv_cliente)   as cliente_completo,			
          a.cod_num_operacion   as ope_vigente,
          a.cod_ope_original    as ope_original,
          a.cod_cctb,
          a.cod_cartera_ope
  from """ + db_plat_tempX + """.tmp_d00_v3_act_ctg a
  join """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_5 b
    on trim(a.cod_cctb) = trim(b.cta_ctable)
      and a.periodo_id = b.periodo_id
  where a.periodo_id = '""" + anomes + """'
    and trim(a.cod_cartera_ope) = 'ACT'
"""

# COMMAND ----------

sqlsafe(vista_activo_5)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 7: Obtencion Univ Tipo Activo 6

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de los activos 6 desde v2_tabmae_mapeo_normativo y TBU
# MAGIC
# MAGIC **Tabla de salida**: bci20_basenorma_tmp_cta_ain_6 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 	
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **mapeo_norma_id**: Numero del mapeo de la norma
# MAGIC - **cta_ctable**: Cuenta contable

# COMMAND ----------

vista_cta_ain_6 = """ insert into """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_6
  select  substring(b.fecha_informada, 1, 6)    as periodo_id,
          a.registro_id,
          a.mapeo_norma_id, 				
          b.cta_ctable
  from """ + db_platinumX + """.tabmae_mapeo_normativo a
  join """ + db_slv_ParametricasX + """.TBU b
    on trim(a.cod_cta_contable) = trim(b.cta_cmf)
  where b.fecha_informada = '""" + date_proceso_2 + """'
    and a.mapeo_norma_id = 1216
    and trim(a.cod_considera_cta) = '1'
    and trim(b.cta_ctable) not in (select trim(cod_cta_contable)  as cod_cta_contable
                                  from """ + db_platinumX + """.tabmae_mapeo_normativo 
                                   where mapeo_norma_id = 1216 and trim(cod_considera_cta) = '-1' and trim(cod_cta_contable) <> '0')
    and substring(trim(b.cta_ctable), -1, 1) != 'C'
"""


# COMMAND ----------

sql_safe(vista_cta_ain_6)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 8: Obtencion Operaciones Tipo Activo 6

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones y clientes del tipo activo 6 desde tmp_d00_v3_act_ctg y bci20_basenorma_tmp_cta_ain_6
# MAGIC
# MAGIC **Tabla de salida**: bci20_basenorma_tmp_c12_reg1 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **activo_id**: Codigo tipo activo	
# MAGIC - **cliente_rut**: Rut del cliente
# MAGIC - **cliente_dv**: Digito verificador del cliente
# MAGIC - **ope_vigente**: Codigo operacion vigente (cod_num_operacion)
# MAGIC - **ope_original**: Codigo operacion original (cod_ope_original)
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cartera_ope**: Codigo tipo de cartera operacional

# COMMAND ----------

vista_activo_6 = """ insert into """ + db_plat_tempX + """.bci20_basenorma_tmp_c12_reg1
  select  a.periodo_id,
          b.registro_id,
          'C12'                 as categoria,
          6                     as tipo_activo,
          1216                  as activo_id,
          a.rut_cliente         as cliente_rut,	
          a.dv_cliente          as cliente_dv,	
          concat(a.rut_cliente, a.dv_cliente)   as cliente_completo,		
          a.cod_num_operacion   as ope_vigente,
          a.cod_ope_original    as ope_original,
          a.cod_cctb,
          a.cod_cartera_ope
  from """ + db_plat_tempX + """.tmp_d00_v3_act_ctg a
  join """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_6 b
    on trim(a.cod_cctb) = trim(b.cta_ctable)
      and a.periodo_id = b.periodo_id
  where a.periodo_id = '""" + anomes + """'
    and trim(a.cod_cartera_ope) = 'ACT'
"""

# COMMAND ----------

sqlsafe(vista_activo_6)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 9: Obtencion Univ Tipo Activo 7

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de los activos 7 desde v2_tabmae_mapeo_normativo y TBU
# MAGIC
# MAGIC **Tabla de salida**: bci20_basenorma_tmp_cta_ain_7 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 	
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **mapeo_norma_id**: Numero del mapeo de la norma
# MAGIC - **cta_ctable**: Cuenta contable

# COMMAND ----------

vista_cta_ain_7 = """ insert into """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_7
  select  substring(b.fecha_informada, 1, 6)    as periodo_id,
          a.registro_id,
          a.mapeo_norma_id, 				
          b.cta_ctable
  from """ + db_platinumX + """.tabmae_mapeo_normativo a
  join """ + db_slv_ParametricasX + """.TBU b
    on trim(a.cod_cta_contable) = trim(b.cta_cmf)
  where b.fecha_informada = '""" + date_proceso_2 + """'
    and a.mapeo_norma_id = 1217
    and trim(a.cod_considera_cta) = '1'
    and trim(b.cta_ctable) not in (select trim(cod_cta_contable) as cod_cta_contable
                              from """ + db_platinumX + """.tabmae_mapeo_normativo 
                             where mapeo_norma_id = 1217 and trim(cod_considera_cta) = '-1' and trim(cod_cta_contable) != '0')
    and substring(trim(b.cta_ctable), -1, 1) != 'C'
"""


# COMMAND ----------

sql_safe(vista_cta_ain_7)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 10: Obtencion Operaciones Tipo Activo 7

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones y clientes del tipo activo 7 desde tmp_d00_v3_act_ctg y bci20_basenorma_tmp_cta_ain_7
# MAGIC
# MAGIC **Tabla de salida**: bci20_basenorma_tmp_c12_reg1 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **activo_id**: Codigo tipo activo	
# MAGIC - **cliente_rut**: Rut del cliente
# MAGIC - **cliente_dv**: Digito verificador del cliente
# MAGIC - **ope_vigente**: Codigo operacion vigente (cod_num_operacion)
# MAGIC - **ope_original**: Codigo operacion original (cod_ope_original)
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cartera_ope**: Codigo tipo de cartera operacional

# COMMAND ----------

vista_activo_7 = """ insert into """ + db_plat_tempX + """.bci20_basenorma_tmp_c12_reg1
  select  a.periodo_id,
          b.registro_id,
          'C12'                 as categoria,
          7                     as tipo_activo,
          1217                  as activo_id,
          a.rut_cliente         as cliente_rut,	
          a.dv_cliente          as cliente_dv,
          concat(a.rut_cliente, a.dv_cliente)   as cliente_completo,			
          a.cod_num_operacion   as ope_vigente,
          a.cod_ope_original    as ope_original,
          a.cod_cctb,
          a.cod_cartera_ope
  from """ + db_plat_tempX + """.tmp_d00_v3_act_ctg a
  join """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_7 b
    on trim(a.cod_cctb) = trim(b.cta_ctable)
      and a.periodo_id = b.periodo_id
  where a.periodo_id = '""" + anomes + """'
    and trim(a.cod_cartera_ope) = 'ACT'
"""

# COMMAND ----------

sqlsafe(vista_activo_7)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 11: Obtencion Univ Tipo Activo 8

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de los activos 8 desde v2_tabmae_mapeo_normativo y TBU
# MAGIC
# MAGIC **Tabla de salida**: bci20_basenorma_tmp_cta_ain_8 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 	
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **mapeo_norma_id**: Numero del mapeo de la norma
# MAGIC - **cta_ctable**: Cuenta contable

# COMMAND ----------

vista_cta_ain_8 = """ insert into """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_8
  select  substring(b.fecha_informada, 1, 6)    as periodo_id,
          a.registro_id,
          a.mapeo_norma_id, 				
          b.cta_ctable
  from """ + db_platinumX + """.tabmae_mapeo_normativo a
  join """ + db_slv_ParametricasX + """.TBU b
    on trim(a.cod_cta_contable) = trim(b.cta_cmf)
  where b.fecha_informada = '""" + date_proceso_2 + """'
    and a.mapeo_norma_id = 1218
    and trim(a.cod_considera_cta) = '1'
    and trim(b.cta_ctable) not in (select trim(cod_cta_contable) as cod_cta_contable
                                   from """ + db_platinumX + """.tabmae_mapeo_normativo 
                                   where mapeo_norma_id = 1218 and trim(cod_considera_cta) = '-1' and trim(cod_cta_contable) != '0')
    and substring(trim(b.cta_ctable), -1, 1) != 'C'
"""

# COMMAND ----------

sql_safe(vista_cta_ain_8)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 12: Obtencion Operaciones Tipo Activo 8

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones y clientes del tipo activo 8 desde tmp_d00_v3_act_ctg y bci20_basenorma_tmp_cta_ain_8
# MAGIC
# MAGIC **Tabla de salida**: bci20_basenorma_tmp_c12_reg1 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **activo_id**: Codigo tipo activo	
# MAGIC - **cliente_rut**: Rut del cliente
# MAGIC - **cliente_dv**: Digito verificador del cliente
# MAGIC - **ope_vigente**: Codigo operacion vigente (cod_num_operacion)
# MAGIC - **ope_original**: Codigo operacion original (cod_ope_original)
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cartera_ope**: Codigo tipo de cartera operacional

# COMMAND ----------

vista_activo_8 = """ insert into """ + db_plat_tempX + """.bci20_basenorma_tmp_c12_reg1
  select  a.periodo_id,
          b.registro_id,
          'C12'                 as categoria,
          8                     as tipo_activo,
          1218                  as activo_id,
          a.rut_cliente         as cliente_rut,	
          a.dv_cliente          as cliente_dv,
          concat(a.rut_cliente, a.dv_cliente)   as cliente_completo,			
          a.cod_num_operacion   as ope_vigente,
          a.cod_ope_original    as ope_original,
          a.cod_cctb,
          a.cod_cartera_ope
  from """ + db_plat_tempX + """.tmp_d00_v3_act_ctg a
  join """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_8 b
    on trim(a.cod_cctb) = trim(b.cta_ctable)
      and a.periodo_id = b.periodo_id
  where a.periodo_id = '""" + anomes + """'
    and trim(a.cod_cartera_ope) = 'ACT'
"""

# COMMAND ----------

sqlsafe(vista_activo_8)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 13: Obtencion Univ Tipo Activo 9

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de los activos 9 desde v2_tabmae_mapeo_normativo y TBU
# MAGIC
# MAGIC **Tabla de salida**: bci20_basenorma_tmp_cta_ain_9 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 	
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **mapeo_norma_id**: Numero del mapeo de la norma
# MAGIC - **cta_ctable**: Cuenta contable

# COMMAND ----------

vista_cta_ain_9 = """ insert into """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_9
  select  substring(b.fecha_informada, 1, 6)    as periodo_id,
          a.registro_id,
          a.mapeo_norma_id, 				
          b.cta_ctable
  from """ + db_platinumX + """.tabmae_mapeo_normativo a
  join """ + db_slv_ParametricasX + """.TBU b
    on trim(a.cod_cta_contable) = trim(b.cta_cmf)
  where b.fecha_informada = '""" + date_proceso_2 + """'
    and a.mapeo_norma_id = 1219
    and trim(a.cod_considera_cta) = '1'
    and trim(b.cta_ctable) not in (select trim(cod_cta_contable) as cod_cta_contable 
                                    from """ + db_platinumX + """.tabmae_mapeo_normativo 
                                   where mapeo_norma_id = 1219 and trim(cod_considera_cta) = '-1' and trim(cod_cta_contable) != '0')
    and substring(trim(b.cta_ctable), -1, 1) != 'C'
  union all
  select  '""" + anomes + """'    as periodo_id,
          registro_id,
          mapeo_norma_id, 				
          cod_cta_contable_int
  from """ + db_platinumX + """.tabmae_mapeo_normativo
  where mapeo_norma_id = 1219
    and trim(cod_considera_cta) = '1'
    and trim(cod_tipo_activo) = '9'
"""

# COMMAND ----------

sql_safe(vista_cta_ain_9)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 14: Obtencion Operaciones Tipo Activo 9

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones y clientes del tipo activo 9 desde tmp_d00_v3_act_ctg y bci20_basenorma_tmp_cta_ain_9
# MAGIC
# MAGIC **Tabla de salida**: bci20_basenorma_tmp_c12_reg1 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **activo_id**: Codigo tipo activo	
# MAGIC - **cliente_rut**: Rut del cliente
# MAGIC - **cliente_dv**: Digito verificador del cliente
# MAGIC - **ope_vigente**: Codigo operacion vigente (cod_num_operacion)
# MAGIC - **ope_original**: Codigo operacion original (cod_ope_original)
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cartera_ope**: Codigo tipo de cartera operacional

# COMMAND ----------

vista_activo_9 = """ insert into """ + db_plat_tempX + """.bci20_basenorma_tmp_c12_reg1
  select  a.periodo_id,
          b.registro_id,
          'C12'                 as categoria,
          9                     as tipo_activo,
          1219                  as activo_id,
          a.rut_cliente         as cliente_rut,	
          a.dv_cliente          as cliente_dv,
          concat(a.rut_cliente, a.dv_cliente)   as cliente_completo,			
          a.cod_num_operacion   as ope_vigente,
          a.cod_ope_original    as ope_original,
          a.cod_cctb,
          a.cod_cartera_ope
  from """ + db_plat_tempX + """.tmp_d00_v3_act_ctg a
  join """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_9 b
    on trim(a.cod_cctb) = trim(b.cta_ctable)
      and a.periodo_id = b.periodo_id
  where a.periodo_id = '""" + anomes + """'
    and trim(a.cod_cartera_ope) = 'ACT'
"""

# COMMAND ----------

sqlsafe(vista_activo_9)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 15: Obtencion Univ Tipo Activo 20

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de los activos 20 desde v2_tabmae_mapeo_normativo y TBU
# MAGIC
# MAGIC **Tabla de salida**: bci20_basenorma_tmp_cta_ain_20 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 	
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **mapeo_norma_id**: Numero del mapeo de la norma
# MAGIC - **cta_ctable**: Cuenta contable

# COMMAND ----------

vista_cta_ain_20 = """ insert into """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_20
  select  substring(b.fecha_informada, 1, 6)    as periodo_id,
          a.registro_id,
          a.mapeo_norma_id, 				
          b.cta_ctable
  from """ + db_platinumX + """.tabmae_mapeo_normativo a
  join """ + db_slv_ParametricasX + """.TBU b
    on trim(a.cod_cta_contable) = trim(b.cta_cmf)
  where b.fecha_informada = '""" + date_proceso_2 + """'
    and a.mapeo_norma_id = 12120
    and trim(a.cod_considera_cta) = '1'
    and substring(trim(b.cta_ctable), -1, 1) != 'C'
  union all
  select  '""" + anomes + """'                  as periodo_id,
          registro_id,
          mapeo_norma_id, 				
          cod_cta_contable                       as cta_ctable
  from """ + db_platinumX + """.tabmae_mapeo_normativo
  where mapeo_norma_id = 12120
    and trim(cod_considera_cta) = '1'
    and trim(cod_cta_contable) != '0'
"""
 

# COMMAND ----------

sqlsafe(vista_cta_ain_20)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 16: Obtencion Operaciones Tipo Activo 20

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones y clientes del tipo activo 20 desde tmp_d00_v3_act_ctg y bci20_basenorma_tmp_cta_ain_20
# MAGIC
# MAGIC **Tabla de salida**: bci20_basenorma_tmp_c12_reg1 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **activo_id**: Codigo tipo activo	
# MAGIC - **cliente_rut**: Rut del cliente
# MAGIC - **cliente_dv**: Digito verificador del cliente
# MAGIC - **ope_vigente**: Codigo operacion vigente (cod_num_operacion)
# MAGIC - **ope_original**: Codigo operacion original (cod_ope_original)
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cartera_ope**: Codigo tipo de cartera operacional

# COMMAND ----------

vista_activo_20 = """ insert into """ + db_plat_tempX + """.bci20_basenorma_tmp_c12_reg1
  select  a.periodo_id,
          b.registro_id,
          'C12'                 as categoria,
          20                    as tipo_activo,
          12120                 as activo_id,
          a.rut_cliente         as cliente_rut,	
          a.dv_cliente          as cliente_dv,
          concat(a.rut_cliente, a.dv_cliente)   as cliente_completo,			
          a.cod_num_operacion   as ope_vigente,
          a.cod_ope_original    as ope_original,
          a.cod_cctb,
          a.cod_cartera_ope
  from """ + db_plat_tempX + """.tmp_d00_v3_act_ctg a
  join """ + db_plat_tempX + """.bci20_basenorma_tmp_cta_ain_20 b
    on trim(a.cod_cctb) = trim(b.cta_ctable)
      and a.periodo_id = b.periodo_id
  where a.periodo_id = '""" + anomes + """'
    and trim(a.cod_cartera_ope) = 'ACT'
"""

# COMMAND ----------

sqlsafe(vista_activo_20)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 17: Obtencion Univ Tipo Activo 21

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de los activos 21 desde tmp_d00_v3_act_ctg
# MAGIC
# MAGIC **Tabla de salida**: bci20_basenorma_tmp_tipo_ope_21 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM	
# MAGIC - **activo_id**: Codigo tipo activo	
# MAGIC - **cliente_rut**: Rut del cliente
# MAGIC - **cliente_dv**: Digito verificador del cliente
# MAGIC - **ope_vigente**: Codigo operacion vigente (cod_num_operacion)
# MAGIC - **ope_original**: Codigo operacion original (cod_ope_original)
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cartera_ope**: Codigo tipo de cartera operacional
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00

# COMMAND ----------

vista_tipo_ope_21 = """ insert into """ + db_plat_tempX + """.bci20_basenorma_tmp_tipo_ope_21
  select  periodo_id,
          12121               as activo_id,
          rut_cliente         as cliente_rut,	
          dv_cliente          as cliente_dv,			
          cod_num_operacion   as ope_vigente,
          cod_ope_original    as ope_original,
          cod_cctb,
          cod_cartera_ope,
          cod_tipo_ope
  from """ + db_plat_tempX + """.tmp_d00_v3_act_ctg
  where trim(cod_tipo_ope) in ('PAP325','PAP328','PAP322','CON676')
    and periodo_id = '""" + anomes + """'
    and trim(cod_cartera_ope) = 'ACT'
"""

# COMMAND ----------

sqlsafe(vista_tipo_ope_21)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 18: Obtencion Operaciones Tipo Activo 21

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion del registro de cada operacion activo 21 desde bci20_basenorma_tmp_tipo_ope_21 y v2_tabmae_mapeo_normativo
# MAGIC
# MAGIC **Tabla de salida**: bci20_basenorma_tmp_c12_reg1 (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM
# MAGIC - **registro_id**: Numero del tipo de registro	
# MAGIC - **activo_id**: Codigo tipo activo	
# MAGIC - **cliente_rut**: Rut del cliente
# MAGIC - **cliente_dv**: Digito verificador del cliente
# MAGIC - **ope_vigente**: Codigo operacion vigente (cod_num_operacion)
# MAGIC - **ope_original**: Codigo operacion original (cod_ope_original)
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **cod_cartera_ope**: Codigo tipo de cartera operacional

# COMMAND ----------

vista_activo_21 = """ insert into """ + db_plat_tempX + """.bci20_basenorma_tmp_c12_reg1
  select  a.periodo_id,
          b.registro_id,
          'C12'                 as categoria,
          21                    as tipo_activo,
          a.activo_id,
          a.cliente_rut,	
          a.cliente_dv,	
          concat(a.cliente_rut, a.cliente_dv)   as cliente_completo,		
          a.ope_vigente,
          a.ope_original,
          a.cod_cctb,
          a.cod_cartera_ope
  from """ + db_plat_tempX + """.bci20_basenorma_tmp_tipo_ope_21 a
  join """ + db_platinumX + """.tabmae_mapeo_normativo b
    on trim(a.cod_cctb) = trim(b.cod_cta_contable_int)
      and trim(a.activo_id) = trim(b.mapeo_norma_id)
      and trim(b.cod_tipo_activo) = '21'
"""

# COMMAND ----------

sql_safe(vista_activo_21)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 18: Eliminacion Datos Tabla 

# COMMAND ----------

# MAGIC %md
# MAGIC Se elimina los datos de la tabla base_normativa_v2_ft correspondiente a la fecha de proceso que se va a insertar posteriormente
# MAGIC
# MAGIC **Tabla de salida**: base_normativa_v2_ft (tabla de salida)

# COMMAND ----------

delete_base_normativa_v2_ft = """delete from """ + db_plat_tempX + """.base_normativa_v2_ft 
                                 where fec_proceso = '""" + date_proceso + """' and trim(cod_activo) in (1211, 1212, 1215, 1216, 1217, 1218, 1219, 12120, 12121) """

# COMMAND ----------

sqlsafe(delete_base_normativa_v2_ft)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 19: Insercion Datos Tabla base_normativa_v2_ft

# COMMAND ----------

# MAGIC %md
# MAGIC Se prepara e inserta los datos a la tabla base_normativa_v2_ft desde la vista bci20_basenorma_tmp_c12_reg1
# MAGIC
# MAGIC **Tabla de salida**: base_normativa_v2_ft (tabla de salida)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM
# MAGIC - **num_registro**: Numero del tipo de registro	
# MAGIC - **cod_activo**: Codigo tipo activo				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente					
# MAGIC - **cod_num_operacion**: Número de la operación 
# MAGIC - **cod_ope_original**: Numero operación original
# MAGIC - **cod_cta_contable**: Cuenta contable
# MAGIC - **fec_proceso**: Fecha de cierre

# COMMAND ----------

insert_base_normativa_v2_ft = """ insert into """ + db_plat_tempX + """.base_normativa_v2_ft
  select  periodo_id,
          registro_id                       as num_registro,
          categoria                         as cod_categoria,
          tipo_activo                       as num_tipo_activo,
          activo_id                         as cod_activo,
          cliente_rut                       as rut_cliente,	
          cliente_dv                        as dv_cliente,			
          ope_vigente                       as cod_num_operacion,	
          ope_original                      as cod_ope_original,	
          cod_cctb                          as cod_cta_contable,    
          '""" + date_proceso + """'        as fec_proceso
  from """ + db_plat_tempX + """.bci20_basenorma_tmp_c12_reg1
"""

# COMMAND ----------

sqlsafe(insert_base_normativa_v2_ft)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Conteo de registros insertados en tabla de salida

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla base_normativa_v2_ft

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """ + db_plat_tempX + """.base_normativa_v2_ft  where fec_proceso = '""" + date_proceso + """' and trim(cod_activo) in (1211, 1212, 1215, 1216, 1217, 1218, 1219, 12120)  """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla temporal base_normativa_v2_ft")

# COMMAND ----------

# MAGIC %md
# MAGIC ## Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")